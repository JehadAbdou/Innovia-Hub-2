import axios from "axios";
const API_BASE_URL = import.meta.env.VITE_API_URL;
const api = axios.create({
  baseURL: API_BASE_URL,
});

// Helper: normalize timeslot formats to canonical "HH-HH" used by backend
export const normalizeTimeSlot = (timeSlot: string) => {
  if (!timeSlot) return timeSlot;
  const parts = timeSlot.split("-");
  if (parts.length !== 2) return timeSlot.trim();

  const normalizePart = (p: string) =>
    p.trim().replace(":00", "").replace(":", "");
  return `${normalizePart(parts[0])}-${normalizePart(parts[1])}`;
};

//AUTH - Register
export const registerUser = async (
  email: string,
  password: string,
  name: string
) => {
  const res = await api.post("api/auth/register", { email, password, name });
  console.log(res.data);
  return res.data;
};

//AUTH - Log in
export const loginUser = async (email: string, password: string) => {
  const res = await api.post("api/auth/login", { email, password });
  console.log("API Response:", res.data);
  localStorage.setItem("userName", res.data.userName);
  localStorage.setItem("userId", res.data.id);
  return res.data;
};

//BOOKING - Get all bookings
export const getAllBookings = async (token: string) => {
  const res = await api.get("api/booking", {
    headers: { Authorization: `Bearer ${token}` },
  });
  return res.data;
};
// Booking Get filtered Bookings
export const getFilteredBookings = async (token: string, date: Date | null) => {
  const dateStr = date!.toISOString().slice(0, 10);
  const res = await api.get(`api/booking/date?date=${dateStr}`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  return res.data;
};
//BOOKING - Get a users booking
export const getUserBookings = async (userId: string, token: string) => {
  const res = await api.get(`api/booking/user/${userId}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
  return res.data;
};
const formatDateLocal = (date: string | Date) => {
  const d = date instanceof Date ? date : new Date(date);
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, "0"); // months are 0-based
  const day = String(d.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
};

//TIMESlOTS
export const getFreeSlots = async (
  date: string | Date,
  resourceTypeId: number,
  token: string
) => {
  const formattedDate = formatDateLocal(date);
  const payload = { date: formattedDate };
  console.log("🚀 Sending payload to backend:", payload);

  const res = await api.post<string[]>(
    `api/booking/${resourceTypeId}/freeSlots`,
    payload,
    { headers: { Authorization: `Bearer ${token}` } }
  );

  return res.data;
};

//BOOKING - Create a booking
export const createBooking = async (
  booking: {
    date: string;
    timeSlot: string;
    resourceTypeId: number;
    userId: string;
  },
  token: string
) => {
  // Normalize timeslot client-side to increase chance of matching backend format
  booking.timeSlot = normalizeTimeSlot(booking.timeSlot);

  const res = await api.post("api/booking", booking, {
    headers: { Authorization: `Bearer ${token} ` },
  });
  return res.data;
};

//BOOKING - Remove a booking
export const deleteBooking = async (bookingId: number, token: string) => {
  const res = await api.delete(`api/booking/${bookingId}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
  return res.data;
};

//BOOKING - Change Resource Status
export const changeResourceStatus = async (
  resourceId: number,
  token: string
) => {
  const res = await api.patch(
    `api/booking/resource/${resourceId}`,
    {},
    {
      headers: { Authorization: `Bearer ${token}` },
    }
  );
  return res.data;
};
export const getAllResources = async (token: string) => {
  const res = await api.get("api/resource", {
    headers: { Authorization: `Bearer ${token}` },
  });
  return res.data;
};
export const getResourceById = async (token: string, id: number) => {
  const res = await api.get(`/resource/${id}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return res.data;
};

//USERS - Ge All Users
export const getAllUsers = async (
  token: string,
  filter?: { name?: string; email?: string }
) => {
  const res = await api.get("api/users", {
    headers: {
      Authorization: `Bearer ${token}`,
      Accept: "application/json",
    },
    params: filter,
  });
  return res.data;
};

//USERS - Get specific user
export const getUserById = async (id: string, token: string) => {
  const res = await api.get(`api/users/${id}`, {
    headers: {
      Authorization: `Bearer ${token}`,
      Accept: "application/json",
    },
  });
  return res.data;
};

//USERS - Update user
export const updateUserById = async (
  id: string,
  token: string,
  name: string,
  email: string
) => {
  const res = await api.post(
    `api/users/${id}`,
    { email, name },
    {
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
    }
  );
  return res.data;
};

export const deleteUserById = async (id: string, token: string) => {
  const res = await api.delete(`api/users/${id}`, {
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  });
  return res.data;
};

export const sendRequestToAI = async (question: string, token: string) => {
  const res = await api.post(
    `api/chat`,
    { question },
    {
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-type": "application/json",
      },
    }
  );
  return res.data;
};

export const confirmBooking = async (token: string) => {
  const res = await api.post(
    `api/chat/confirmAction`,
    { Confirm: true },
    {
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
    }
  );
  return res.data;
};

export const cancelBooking = async (token: string) => {
  // The backend exposes /chat/confirmAction to confirm or cancel (Confirm: true/false).
  const res = await api.post(
    `api/chat/confirmAction`,
    { Confirm: false },
    {
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
    }
  );
  return res.data;
};

export const speak = async (text: string, token: string) => {
  const res = await api.post(
    "api/chat/speak",
    { text },
    {
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      responseType: "arraybuffer",
    }
  );

  const blob = new Blob([res.data], { type: "audio/mpeg" });
  const url = URL.createObjectURL(blob);
  const audio = new Audio(url);
  audio.play();

  return audio; 
};

// DEVICES - Get all devices

export const getDevices  = async ()=>{
  const res = await api.get("api/sensors/devices",
    {
      headers: {
        "Content-Type": "application/json",
      },
    }
  );
  return res.data;
}

// RULES - Create rule
export const createDeviceRule = async (
  token: string,
  tenantId: string,
  deviceId: string,
  type: string,
  op: string,
  threshold: number,
  cooldownSeconds: number,
  enabled: boolean
) => {
  const res = await api.post(
    'api/sensors/rules',
    {
      tenantId,
      deviceId,
      type,
      op,
      threshold,
      cooldownSeconds,
      enabled
    },
    {
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
    }
  );
  return res.data;
};

// RULES - Get all rules
export const getAllRules = async (token: string) => {
  const res = await api.get('api/sensors/rules', {
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  });
  return res.data;
};

// RULES - Update rule
export const updateDeviceRule = async (
  ruleId: string,
  token: string,
  tenantId: string,
  deviceId: string,
  type: string,
  op: string,
  threshold: number,
  cooldownSeconds: number,
  enabled: boolean
) => {
  const res = await api.put(
    `api/sensors/rules/${ruleId}`,
    {
      tenantId,
      deviceId,
      type,
      op,
      threshold,
      cooldownSeconds,
      enabled
    },
    {
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
    }
  );
  return res.data;
};

// RULES - Delete rule
export const deleteDeviceRule = async (ruleId: string, token: string) => {
  const res = await api.delete(`api/sensors/rules/${ruleId}`, {
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  });
  return res.data;
}

// ALERTS - Get all alerts
export const getAllAlerts = async (token: string) => {
  const res = await api.get('api/sensors/alerts', {
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  });
  return res.data;
};
