import { useState, useEffect, useRef } from "react";
import { MessageCircle, X, Mic } from "lucide-react";
import { sendRequestToAI, cancelBooking, confirmBooking } from "../api/api";
import { ToastContainer, toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import { ttsConnection } from "../signalRConnection";
import "../styles/ChatBot.css";

type ChatMessage = { sender: "bot" | "user"; text: string };

export default function ChatBot() {
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([
    { sender: "bot", text: "Hello 👋! How can I help you today?" },
  ]);
  const [input, setInput] = useState("");
  const [showBookingButtons, setShowBookingButtons] = useState(false);
  const [isTyping, setIsTyping] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement | null>(null);

  const audioChunksRef = useRef<ArrayBuffer[]>([]);
  const botTextRef = useRef("");

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, showBookingButtons, isTyping]);

  useEffect(() => {
    const onAudioChunk = (chunkBase64: string) => {
      const bytes = Uint8Array.from(atob(chunkBase64), (c) => c.charCodeAt(0));
      audioChunksRef.current.push(bytes.buffer.slice(0));

      const totalChunks = 20;
      const revealLength = Math.floor(
        (audioChunksRef.current.length / totalChunks) *
          botTextRef.current.length
      );
      const partialText = botTextRef.current.slice(0, revealLength);

      setMessages((prev) => {
        const newMessages = [...prev];
        newMessages[newMessages.length - 1] = {
          sender: "bot",
          text: partialText,
        };
        return newMessages;
      });
    };

    const onAudioDone = () => {
      const audioBlob = new Blob(audioChunksRef.current, {
        type: "audio/mpeg",
      });
      const url = URL.createObjectURL(audioBlob);
      const audio = new Audio(url);
      const botText = botTextRef.current;
      const chars = botText.length;

      audioChunksRef.current = [];
      audio.play();

      const updateText = () => {
        if (!audio.duration) {
          requestAnimationFrame(updateText);
          return;
        }

        const progress = audio.currentTime / audio.duration;
        const lengthToShow = Math.floor(progress * chars);
        const partialText = botText.slice(0, lengthToShow);

        setMessages((prev) => {
          const newMessages = [...prev];
          newMessages[newMessages.length - 1] = {
            sender: "bot",
            text: partialText,
          };
          return newMessages;
        });

        if (!audio.paused && audio.currentTime < audio.duration)
          requestAnimationFrame(updateText);
      };

      audio.onplay = () => requestAnimationFrame(updateText);
      audio.onended = () => {
        setMessages((prev) => {
          const newMessages = [...prev];
          newMessages[newMessages.length - 1] = {
            sender: "bot",
            text: botText,
          };
          return newMessages;
        });
        setIsTyping(false);
      };
    };

    ttsConnection.on("audioChunk", onAudioChunk);
    ttsConnection.on("audioDone", onAudioDone);

    ttsConnection
      .start()
      .catch((err) => console.error("SignalR connection error:", err));

    return () => {
      ttsConnection.off("audioChunk", onAudioChunk);
      ttsConnection.off("audioDone", onAudioDone);
    };
  }, []);

  const handleSend = async () => {
    if (!input.trim() || isTyping) return;

    setMessages((prev) => [...prev, { sender: "user", text: input }]);
    const userInput = input.trim();
    setInput("");
    setIsTyping(true);

    try {
      const token = localStorage.getItem("token");
      if (!token) return;

      const response = await sendRequestToAI(userInput, token);
      const botText = response.answer || "I didn't quite understand that...";
      botTextRef.current = botText;

      setMessages((prev) => [...prev, { sender: "bot", text: "" }]);
      ttsConnection.invoke("SendTextToTts", botText);

      if (response.showBookingButtons || response.pendingBooking)
        setShowBookingButtons(true);
    } catch (err) {
      console.error(err);
      setMessages((prev) => [
        ...prev,
        {
          sender: "bot",
          text: "Oops, something went wrong. Please try again.",
        },
      ]);
      setIsTyping(false);
    }
  };

  const handleSendVoice = async (transcript: string) => {
    setInput("");
    setIsTyping(true);

    try {
      const token = localStorage.getItem("token");
      if (!token) return;

      const response = await sendRequestToAI(transcript, token);
      const botText = response.answer || "I didn't quite understand that...";
      botTextRef.current = botText;

      setMessages((prev) => [...prev, { sender: "bot", text: "" }]);
      ttsConnection.invoke("SendTextToTts", botText);

      if (response.showBookingButtons || response.pendingBooking)
        setShowBookingButtons(true);
    } catch (err) {
      console.error(err);
      setMessages((prev) => [
        ...prev,
        {
          sender: "bot",
          text: "Oops, something went wrong. Please try again.",
        },
      ]);
      setIsTyping(false);
    }
  };

  const handleVoice = () => {
    if (isTyping) return;

    const SpeechRecognition =
      (window as any).SpeechRecognition ||
      (window as any).webkitSpeechRecognition;
    if (!SpeechRecognition)
      return alert("Speech recognition is not supported in this browser.");

    const recognition = new SpeechRecognition();
    recognition.lang = "en-US";
    recognition.interimResults = true;
    recognition.continuous = false;

    const tempMessageIndex = messages.length;
    setMessages((prev) => [
      ...prev,
      { sender: "user", text: "🎤 Listening..." },
    ]);

    recognition.onresult = (event: any) => {
      let fullTranscript = "";
      for (let i = 0; i < event.results.length; i++)
        fullTranscript += event.results[i][0].transcript;

      setMessages((prev) => {
        const newMessages = [...prev];
        newMessages[tempMessageIndex] = {
          sender: "user",
          text: fullTranscript,
        };
        return newMessages;
      });

      const result = event.results[event.results.length - 1];
      if (result.isFinal) handleSendVoice(fullTranscript);
    };

    recognition.onerror = (event: any) => {
      console.error("Speech recognition error:", event.error);
      setMessages((prev) => prev.slice(0, tempMessageIndex));
    };

    recognition.start();
  };

  return (
    <div className="chatbot-container">
      {!isOpen && (
        <button className="chatbot-toggle" onClick={() => setIsOpen(true)}>
          <MessageCircle size={28} />
        </button>
      )}

      {isOpen && (
        <div className="chatbot-window">
          <div className="chatbot-header">
            Innovia AI-bot
            <button className="chatbot-close" onClick={() => setIsOpen(false)}>
              <X size={20} />
            </button>
          </div>

          <div className="chatbot-messages">
            {messages.map((msg, idx) => (
              <div key={idx} className={`chatbot-message ${msg.sender}`}>
                {msg.text}
              </div>
            ))}

            {isTyping && !messages[messages.length - 1]?.text && (
              <div className="chatbot-message bot">
                <div className="typing-indicator">
                  <span></span>
                  <span></span>
                  <span></span>
                </div>
              </div>
            )}

            {showBookingButtons && !isTyping && (
              <div className="chatbot-booking-buttons">
                <button
                  className="chatbot-confirm-btn"
                  onClick={async (e) => {
                    e.preventDefault();
                    const token = localStorage.getItem("token") ?? "";
                    setIsTyping(true);
                    try {
                      const res = await confirmBooking(token);
                      const msg = res?.message ?? res?.error ?? "Confirmed.";
                      setMessages((prev) => [
                        ...prev,
                        { sender: "bot", text: msg },
                      ]);
                      toast.success(msg);
                    } catch (err) {
                      console.error(err);
                      setMessages((prev) => [
                        ...prev,
                        { sender: "bot", text: "Error during confirmation." },
                      ]);
                      toast.error("Error during confirmation.");
                    } finally {
                      setShowBookingButtons(false);
                      setIsTyping(false);
                    }
                  }}>
                  ✓ Confirm
                </button>

                <button
                  className="chatbot-cancel-btn"
                  onClick={async (e) => {
                    e.preventDefault();
                    const token = localStorage.getItem("token") ?? "";
                    setIsTyping(true);
                    try {
                      const res = await cancelBooking(token);
                      const msg =
                        res?.message ?? res?.error ?? "Action cancelled.";
                      setMessages((prev) => [
                        ...prev,
                        { sender: "bot", text: msg },
                      ]);
                      toast.info(msg);
                    } catch (err) {
                      console.error(err);
                      setMessages((prev) => [
                        ...prev,
                        { sender: "bot", text: "Error during cancellation." },
                      ]);
                      toast.error("Error during cancellation.");
                    } finally {
                      setShowBookingButtons(false);
                      setIsTyping(false);
                    }
                  }}>
                  ✗ Cancel
                </button>
              </div>
            )}

            <div ref={messagesEndRef} />
          </div>

          <div className="chatbot-input-container">
            <input
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder="Type or speak..."
              className="chatbot-input"
              onKeyDown={(e) => e.key === "Enter" && handleSend()}
              disabled={isTyping}
            />
            <button
              className="chatbot-voice"
              onClick={handleVoice}
              disabled={isTyping}>
              <Mic size={20} />
            </button>
            <button
              className="chatbot-send"
              onClick={handleSend}
              disabled={isTyping}>
              Send
            </button>
          </div>

          <ToastContainer position="top-right" autoClose={5000} />
        </div>
      )}
    </div>
  );
}
