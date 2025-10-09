import { useState, useEffect } from "react";
import {
  getUserBookings,
  updateUserById,
  deleteBooking,
  getUserById,
} from "../api/api";
import "../styles/Profile.css";

import type { Booking } from "../Interfaces/types";

const Profile = () => {
  const [userName, setUserName] = useState<string>("");
  const [email, setEmail] = useState<string>("");
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [error, setError] = useState<string>("");

  const parseTimeSlot = (
    date: string,
    timeSlot: string,
    useEndTime = false
  ): number => {
    const [startStr, endStr] = timeSlot.split("-");
    const timeStr = useEndTime ? endStr : startStr;

    let hour = 0;
    let minute = 0;

    if (timeStr.includes(":")) {
      [hour, minute] = timeStr.split(":").map(Number);
    } else {
      hour = Number(timeStr);
    }

    const bookingDate = new Date(date);
    if (isNaN(bookingDate.getTime())) return 0;

    bookingDate.setHours(hour, minute, 0, 0);
    return bookingDate.getTime();
  };

  useEffect(() => {
    const storedName = localStorage.getItem("userName") || "Gäst";
    const storedEmail = localStorage.getItem("email") || "";
    setUserName(storedName || "");
    setEmail(storedEmail || "");

    const fetchBookings = async () => {
      const userId = localStorage.getItem("userId");
      const token = localStorage.getItem("token");

      if (userId && token) {
        try {
          const userBookings = await getUserBookings(userId, token);

          const now = new Date().getTime();

          userBookings.forEach((booking: Booking) => {
            const bookingStartTime = parseTimeSlot(
              booking.date,
              booking.timeSlot
            );
            const bookingEndTime = parseTimeSlot(
              booking.date,
              booking.timeSlot,
              true
            );
            console.log(
              `Bokning: ${booking.date} ${booking.timeSlot}, Start: ${bookingStartTime}, Slut: ${bookingEndTime}`
            );
          });

          // filtrerar kommande bokningar
          const upcomingBookings = userBookings.filter((booking: Booking) => {
            const bookingEndTime = parseTimeSlot(
              booking.date,
              booking.timeSlot,
              true
            ); // Använd sluttiden
            return bookingEndTime >= now;
          });

          // filtrerar tidigare bokningar
          const pastBookings = userBookings.filter((booking: Booking) => {
            const bookingEndTime = parseTimeSlot(
              booking.date,
              booking.timeSlot,
              true
            ); // Använd sluttiden
            return bookingEndTime < now;
          });

          // sorterar kommande bokningar och lägger tidigast först
          const sortedUpcomingBookings = upcomingBookings.sort(
            (a: Booking, b: Booking) => {
              const timeA = parseTimeSlot(a.date, a.timeSlot);
              const timeB = parseTimeSlot(b.date, b.timeSlot);
              return timeA - timeB;
            }
          );

          // sorterar tidigare bokningar
          const sortedPastBookings = pastBookings.sort(
            (a: Booking, b: Booking) => {
              const timeA = parseTimeSlot(a.date, a.timeSlot);
              const timeB = parseTimeSlot(b.date, b.timeSlot);
              return timeB - timeA;
            }
          );

          // uppdaterar state med filtrerade bokningar
          setBookings([...sortedUpcomingBookings, ...sortedPastBookings]);
        } catch (err: any) {
          if (err.response && err.response.status === 404) {
            console.warn("Inga bokningar hittades för användaren.");
            setBookings([]);
          } else {
            console.error("Fel vid hämtning av bokningar:", err);
            setError("Kunde inte hämta bokningar. Försök igen senare.");
          }
        }
      } else {
        setError("Användaren är inte inloggad.");
      }
    };

    fetchBookings();
  }, []);

  // funktion för att avboka bokning
  const handleCancelBooking = async (bookingId: number) => {
    try {
      const token = localStorage.getItem("token");

      if (!token) {
        alert("Du är inte inloggad.");
        return;
      }

      await deleteBooking(bookingId, token);
      alert("Bokningen har avbokats!");

      // uppdaterar bokningar efter avbokning
      setBookings((prevBookings) =>
        prevBookings.filter((booking) => booking.bookingId !== bookingId)
      );
    } catch (error) {
      console.error("Fel vid avbokning:", error);
      alert("Kunde inte avboka bokningen. Försök igen senare.");
    }
  };

  // funktion för att uppdatera profil
  const handleUpdateProfile = async () => {
    try {
      const token = localStorage.getItem("token");
      const userId = localStorage.getItem("userId");

      if (!token || !userId) {
        alert("Du är inte inloggad.");
        return;
      }

      await updateUserById(userId, token, userName, email);
      alert("Profilen har uppdaterats!");

      const updatedUser = await getUserById(userId, token);

      setUserName(updatedUser.userName);
      setEmail(updatedUser.email);
      localStorage.setItem("userName", userName);
      localStorage.setItem("email", email);
    } catch (error) {
      console.error("Fel vid uppdatering av profil:", error);
      alert("Kunde inte uppdatera profil. Försök igen senare.");
    }
  };

  // funktion för att välja bild baserat på resursnamn
  const resourceImages = (resourceName: string): string => {
    if (resourceName.startsWith("VR Headset")) {
      return "./img/vrheadset.png";
    } else if (resourceName.startsWith("Drop-in skrivbord")) {
      return "./img/skrivbord.png";
    } else if (resourceName.startsWith("Mötesrum")) {
      return "./img/motesrum.png";
    } else if (resourceName.startsWith("AI Server")) {
      return "./img/aiserver.png";
    }
    return "/images/innovialogo.png"; //fall back bild om det inte funkar
  };

  return (
    <div className="profileContainer">
      <h1>Min profil</h1>
      <div className="profileSections">
        <div className="bookingsSection">
          <h3>Dina bokningar</h3>
          {error && <p className="error">{error}</p>}
          {bookings.length > 0 ? (
            <>
              <h4>Kommande bokningar</h4>
              <ul>
                {bookings
                  .filter((booking) => {
                    const bookingEndTime = parseTimeSlot(
                      booking.date,
                      booking.timeSlot,
                      true
                    );
                    return bookingEndTime >= new Date().getTime();
                  })
                  .map((booking) => (
                    <li
                      key={
                        booking.bookingId ||
                        `${booking.date}-${booking.timeSlot}`
                      }>
                      <img
                        src={resourceImages(booking.resourceName)}
                        alt={booking.resourceName}
                      />
                      <div>
                        <p>{booking.resourceName}</p>
                        <p>Tid: {booking.timeSlot}</p>
                        <p>Datum: {booking.date}</p>
                      </div>
                      <button
                        className="cancelButton"
                        onClick={() => handleCancelBooking(booking.bookingId)}>
                        AVBOKA
                      </button>
                    </li>
                  ))}
              </ul>

              <h4>Tidigare bokningar</h4>
              <ul>
                {bookings
                  .filter((booking) => {
                    const bookingEndTime = parseTimeSlot(
                      booking.date,
                      booking.timeSlot,
                      true
                    );
                    return bookingEndTime < new Date().getTime();
                  })
                  .map((booking) => (
                    <li
                      key={
                        booking.bookingId ||
                        `${booking.date}-${booking.timeSlot}`
                      }
                      className="pastBooking">
                      <img
                        src={resourceImages(booking.resourceName)}
                        alt={booking.resourceName}
                      />
                      <div>
                        <p>{booking.resourceName}</p>
                        <p>Tid: {booking.timeSlot}</p>
                        <p>Datum: {booking.date}</p>
                      </div>
                      <button className="cancelButton" disabled>
                        AVBOKA
                      </button>
                    </li>
                  ))}
              </ul>
            </>
          ) : (
            !error && <p>Du har inga bokningar ännu.</p>
          )}
        </div>

        {/* redigera profil */}
        <div className="editProfileSection">
          <h3>Redigera din profil</h3>
          <form onSubmit={(e) => e.preventDefault()}>
            <input
              placeholder="Användarnamn"
              type="text"
              value={userName || ""}
              onChange={(e) => setUserName(e.target.value)}
            />
            <input
              placeholder="Email"
              type="email"
              value={email || ""}
              onChange={(e) => setEmail(e.target.value)}
            />
            <button className="updateButton" onClick={handleUpdateProfile}>
              LÄGG TILL
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};

export default Profile;
