import { useState } from "react";
import { loginUser } from "../api/api";
import "../styles/LoginAndRegisterForms.css";
import { useNavigate } from "react-router-dom";
import { useFormMessages } from "../hooks/useFormMessages";

const LoginForm = () => {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const { errorMessage, setErrorMessage, resetMessages } = useFormMessages();
  const navigate = useNavigate();

  // Hantera formulärsubmission
  const handleSubmit = async (event: React.FormEvent) => {
    event?.preventDefault();
    resetMessages();

    // Grundläggande validering
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      setErrorMessage("Ange en giltig e-postadress");
      return;
    }
    if (password.length < 6) {
      setErrorMessage("Lösenordet måste vara minst 6 tecken långt");
      return;
    }
    if (!email || !password) {
      setErrorMessage("Fyll i både email och lösenord");
      return;
    }

    try {
      const { token, role } = await loginUser(email, password);

      // Spara token och användardata
      localStorage.setItem("token", token);
      localStorage.setItem("role", role);
      localStorage.setItem("email", email);

      // Navigera baserat på roll
      navigate("/");
    } catch (error: any) {
      setErrorMessage(error.response?.status === 401 ? "Felaktigt användarnamn eller lösenord" : "Något gick fel, försök igen senare");
    }
  };
  return (
    <div className="formBox">
      <h2 className="formHeading">Logga in</h2>
      <form onSubmit={handleSubmit}>
        <input
          type="email"
          placeholder={"Email"}
          onChange={(e) => setEmail(e.target.value)}
          value={email}
        />
        <input
          type="password"
          placeholder={"Lösenord"}
          onChange={(e) => setPassword(e.target.value)}
          value={password}
        />

        {errorMessage && <p className="errorMessage">{errorMessage}</p>}

        <p>Har du glömt ditt lösenord?</p>

        <button type="submit" className="formBtn">
          Logga in
        </button>
      </form>
    </div>
  );
};
export default LoginForm;
