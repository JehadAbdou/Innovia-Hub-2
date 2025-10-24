export interface FreeSlotsProps {
  resourceId: number;
  date: string | Date;
}
export interface CalendarProps {
  selectedDate: Date;
  onDateChange: (date: Date) => void;
}
export interface ResourceImageAndDateProps {
  imgUrl: string;
  imgAlt: string;
  selectedDate: string;
}

export interface  Sensor{
    id: string;
    tenantId: string;
    roomId: string;
    model: string;
    serial: string;
    status: string;
}