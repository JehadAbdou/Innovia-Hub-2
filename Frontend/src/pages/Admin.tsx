import React, { useState, useEffect } from "react";
import "../styles/admin.css";
import BookingsTab from "../components/BookingsTab";
import UsersTab from "../components/UsersTab";
import ResourcesTab from "../components/ResourcesTab";
import { connection } from "../signalRConnection";
import { ToastContainer, toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import SensorsTab from "../components/SensorsTab";

interface AdminProps {
  token: string;
}

const Admin: React.FC<AdminProps> = ({ token }) => {
  const [activeTab, setActiveTab] = useState("bookings");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const initializeAdmin = async () => {
      try {
        const page = localStorage.getItem("activePage");
        if (page) {
          setActiveTab(page);
        }
        document.body.classList.add("adminBg");
        
        // Give SignalR connection time to establish
        if (connection.state === 'Disconnected') {
          await connection.start();
        }
      } catch (error) {
        console.error('Error initializing admin:', error);
      } finally {
        setIsLoading(false);
      }
    };

    initializeAdmin();
    return () => {
      document.body.classList.remove("adminBg");
    };
  }, []);

  useEffect(() => {
    const handler = (update: any) => {
      toast.info(
        `en ${update.resourceName} har blivit bokad på ${update.date} under ${update.timeSlot}`
      );
      console.log(update);
    };
    connection.on("ReceiveBookingUpdate", handler);
    return () => {
      connection.off("ReceiveBookingUpdate", handler);
    };
  }, []);

  useEffect(() => {
    const handler = (update: any) => {
      toast.info(
        `en ${update.resourceName} har blivit Avbokad på ${update.date} under ${update.timeSlot}`
      );
      console.log(update);
    };
    connection.on("ReceiveDeleteBookingUpdate", handler);
    return () => {
      connection.off("ReceiveDeleteBookingUpdate", handler);
    };
  }, []);

  if (isLoading) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh' 
      }}>
        <div>Laddar admin panel...</div>
      </div>
    );
  }

  return (
    <div className="dashboard">
      <ToastContainer
        position="top-right"
        autoClose={5000}
        hideProgressBar={false}
        newestOnTop={false}
        closeOnClick
        rtl={false}
        pauseOnFocusLoss
        draggable
        pauseOnHover
        theme="light"
      />

      <div className="adminHeaderHolder">
        <header className="header">
          <div>
            <h1>Adminpanel</h1>
          </div>
        </header>
      </div>

      <nav className="tabs">
        <button
          className={`tab ${activeTab === "bookings" ? "active" : ""}`}
          onClick={() => {
            setActiveTab("bookings");
            localStorage.setItem("activePage", "bookings");
          }}>
          BOKNINGAR
        </button>
        <button
          className={`tab ${activeTab === "users" ? "active" : ""}`}
          onClick={() => {
            setActiveTab("users");
            localStorage.setItem("activePage", "users");
          }}>
          ANVÄNDARE
        </button>
        <button
          className={`tab ${activeTab === "resources" ? "active" : ""}`}
          onClick={() => {
            setActiveTab("resources");
            localStorage.setItem("activePage", "resources");
          }}>
          RESURSER
        </button>
         <button
          className={`tab ${activeTab === "sensors" ? "active" : ""}`}
          onClick={() => {
            setActiveTab("sensors");
            localStorage.setItem("activePage", "sensors");
          }}>
          SENSORS
        </button>
      </nav>

      <div className="content">
        <div style={{ display: activeTab === "bookings" ? "block" : "none" }}>
          <BookingsTab token={token} />
        </div>
        <div style={{ display: activeTab === "users" ? "block" : "none" }}>
          <UsersTab token={token} />
        </div>
        <div style={{ display: activeTab === "resources" ? "block" : "none" }}>
          <ResourcesTab token={token} />
        </div>
        <div style={{ display: activeTab === "sensors" ? "block" : "none" }}>
          <SensorsTab token={token} />
        </div>
      </div>
    </div>
  );
};

export default Admin;
