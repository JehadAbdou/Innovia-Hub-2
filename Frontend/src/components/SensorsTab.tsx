import React, { useEffect, useState } from 'react'
import { 
  getDevices, 
  createDeviceRule, 
  updateDeviceRule, 
  getAllRules,
  deleteDeviceRule 
} from '../api/api'
import SensorsCard from './SensorsCard';
import { IOTconnection } from '../signalRConnection';
import { HubConnectionState } from '@microsoft/signalr';
import toast, { Toaster } from 'react-hot-toast';
import '../styles/SensorsTab.css'; // ✅ new CSS file

interface Props {
  token: string;
}

interface RuleData {
  type: string;
  op: string;
  threshold: number;
  cooldownSeconds: number;
  enabled: boolean;
}

interface Rule {
  id: string;
  deviceId: string;
  type: string;
  op: string;
  threshold: number;
  cooldownSeconds: number;
  enabled: boolean;
}

interface RealtimeMeasurement {
  tenantSlug: string;
  deviceId: string;
  type: string;
  value: number;
  time: string;
}

interface DeviceMeasurement {
  deviceId: string;
  temperature?: number;
  co2?: number;
  humidity?: number;
  air_quality_index?: number;
  power_consumption?: number;
  time?: string;
}

interface Alert {
  tenantSlug: string;
  tenantId: string;
  deviceId: string;
  type: string;
  message: string;
  value: number;
  time: string;
  severity: string;
}

const SensorsTab: React.FC<Props> = ({ token }) => {
  const [sensors, setSensors] = useState<any[]>([]);
  const [rules, setRules] = useState<Rule[]>([]);
  const [measurements, setMeasurements] = useState<Map<string, DeviceMeasurement>>(new Map());
  const [loading, setLoading] = useState(true);

  // Fetch sensors and rules
  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const sensorsRes = await getDevices();
        setSensors(sensorsRes);
        const rulesRes = await getAllRules(token);
        setRules(rulesRes);
      } catch (error) {
        console.error("Failed to fetch data:", error);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [token]);

  // Subscribe to IoT Hub for live updates
  useEffect(() => {
    const subscribeIoT = async () => {
      try {
        if (IOTconnection.state === HubConnectionState.Disconnected) {
          await IOTconnection.start();
          await IOTconnection.invoke("JoinTenant", "innovia");
          console.log("✅ Connected to IoT hub for sensors");
        }

        // Alerts
        IOTconnection.on("alertRaised", (alert: Alert) => {
          const sensor = sensors.find(s => s.id === alert.deviceId);
          const sensorName = sensor?.serial || alert.deviceId;
          if (alert.severity === 'critical') {
            toast.error(`🚨 ${alert.message}\nSensor: ${sensorName}`, { duration: 10000 });
          } else if (alert.severity === 'warning') {
            toast(`⚠️ ${alert.message}\nSensor: ${sensorName}`, { 
              duration: 6000,
              icon: '⚠️',
              style: { background: '#fa5959', color: '#000' },
            });
          } else {
            toast(alert.message, { duration: 5000 });
          }
        });

        // Measurements
        IOTconnection.on("measurementReceived", (data: RealtimeMeasurement[] | RealtimeMeasurement) => {
          const newMeasurements = Array.isArray(data) ? data : [data];
          setMeasurements(prev => {
            const newMap = new Map(prev);
            newMeasurements.forEach(m => {
              const existing = newMap.get(m.deviceId) || { deviceId: m.deviceId };
              newMap.set(m.deviceId, {
                ...existing,
                [m.type]: m.value,
                time: m.time,
              });
            });
            return newMap;
          });
        });

      } catch (err) {
        console.error("IoT Hub error:", err);
        toast.error("Failed to connect to IoT Hub");
      }
    };

    subscribeIoT();

    return () => {
      IOTconnection.off("measurementReceived");
      IOTconnection.off("alertRaised");
    };
  }, [sensors]);

  // ✅ Delete a rule instead of a sensor
const handleDeleteRule = async (ruleId: string) => {
  try {
    await deleteDeviceRule(ruleId, token);

    // Remove the deleted rule from local state
    setRules(prev => prev.filter(r => r.id !== ruleId));

    toast.success('Rule deleted successfully!');
  } catch (error) {
    console.error('Failed to delete rule:', error);
    toast.error('Failed to delete rule');
  }
};

 const handleUpdateRule = async (sensorId: string, rule: RuleData) => {
  try {
    const tenantId = "dab37251-fa80-4a67-b7dd-6d65419b709f";
    const existingRule = rules.find(r => r.deviceId === sensorId);

    if (existingRule) {
      // 🟢 Update existing rule
      await updateDeviceRule(
        existingRule.id,
        token,
        tenantId,
        sensorId,
        rule.type,
        rule.op,
        rule.threshold,
        rule.cooldownSeconds,
        rule.enabled
      );
      toast.success("Rule updated successfully!");
    } else {
      // 🟢 Create new rule

      await createDeviceRule(
        token,
        tenantId,
        sensorId,
        rule.type,
        rule.op,
        rule.threshold,
        rule.cooldownSeconds,
        rule.enabled
      );
      
      toast.success("Rule created successfully!");
    }

    // Refresh rules
    const updatedRules = await getAllRules(token);
    setRules(updatedRules);
  } catch (error) {
    console.error("Failed to save rule:", error);
    toast.error("Failed to save rule");
  }
};


  if (loading) {
    return <div className="loading">Loading sensors and rules...</div>;
  }

  return (
    <div className="container">
      <Toaster position="top-right" />
      <h2 className="header">IoT Sensors ({sensors.length})</h2>

      {sensors.length === 0 ? (
        <p className="noData">No sensors found</p>
      ) : (
        sensors.map(sensor => {
          const sensorRule = rules.find(r => r.deviceId === sensor.id);
          const latestMeasurement = measurements.get(sensor.id);
          return (
            <SensorsCard
              key={sensor.id}
              sensor={sensor}
              existingRule={sensorRule || null}
              latestMeasurement={latestMeasurement}
              onDelete={handleDeleteRule} 
              onUpdateRule={handleUpdateRule}
            />
          );
        })
      )}
    </div>
  );
};

export default SensorsTab;
