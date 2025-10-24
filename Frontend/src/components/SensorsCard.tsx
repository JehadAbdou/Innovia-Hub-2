import React, { useState, useEffect } from 'react';
import type { Sensor } from '../Interfaces/Props';
import  '../styles/SensorsCard.css';
interface Props {
  sensor: Sensor;
  onDelete: (ruleId: string) => void;
  onUpdateRule: (sensorId: string, rule: RuleData) => void;
  existingRule?: Rule | null;
  latestMeasurement?: DeviceMeasurement;
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

interface RuleData {
  type: string;
  op: string;
  threshold: number;
  cooldownSeconds: number;
  enabled: boolean;
}

interface Rule {
  id: string;
  type: string;
  op: string;
  threshold: number;
  cooldownSeconds: number;
  enabled: boolean;
}

const SensorsCard: React.FC<Props> = ({ sensor, onDelete, onUpdateRule, existingRule, latestMeasurement }) => {
  const [isEditing, setIsEditing] = useState(false);
  const [ruleData, setRuleData] = useState<RuleData>({
    type: 'temperature',
    op: '>',
    threshold: 28,
    cooldownSeconds: 300,
    enabled: true,
  });

  // Load existing rule data when component mounts or existingRule changes
useEffect(() => {
  if (existingRule) {
    setRuleData({
      type: existingRule.type || 'temperature',
      op: existingRule.op || '>',
      threshold: existingRule.threshold ?? 28,
      cooldownSeconds: existingRule.cooldownSeconds ?? 300,
      enabled: existingRule.enabled ?? true,
    });
  } else {
    // 🟢 ensure defaults always set
    setRuleData(prev => ({
      ...prev,
      op: prev.op || '>',
      type: prev.type || 'temperature'
    }));
  }
}, [existingRule]);

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const { name, value, type } = e.target;
    
    if (type === 'checkbox') {
      const checked = (e.target as HTMLInputElement).checked;
      setRuleData((prev) => ({ ...prev, [name]: checked }));
    } else if (name === 'threshold' || name === 'cooldownSeconds') {
      setRuleData((prev) => ({ ...prev, [name]: parseFloat(value) || 0 }));
    } else {
      setRuleData((prev) => ({ ...prev, [name]: value }));
    }
  };

const handleSaveRule = () => {
  if (!ruleData.op) {
    alert("Please select a valid operator before saving the rule.");
    return;
  }

  onUpdateRule(sensor.id, ruleData);
  setIsEditing(false);
};

  const handleCancel = () => {
    // Reset to existing rule data if canceling
    if (existingRule) {
      setRuleData({
        type: existingRule.type,
        op: existingRule.op,
        threshold: existingRule.threshold,
        cooldownSeconds: existingRule.cooldownSeconds,
        enabled: existingRule.enabled,
      });
    }
    setIsEditing(false);
  };

 const handleDelete = () => {
  if (!existingRule) {
    alert('No rule exists for this sensor.');
    return;
  }

  const ruleName = `${existingRule.type} (${existingRule.op} ${existingRule.threshold})`;
  if (window.confirm(`Are you sure you want to delete this rule for sensor "${sensor.serial}"?`)) {
    console.log(`Deleting rule: ${existingRule.id} - ${ruleName}`);
    onDelete(existingRule.id);
  }
};

  return (
    <div className="sensor-card">
      <h3 className="sensor-title">Sensor: {sensor.serial}</h3>
      
      <div className="sensor-info-section">
        <p><strong>ID:</strong> {sensor.id}</p>
        <p><strong>Model:</strong> {sensor.model}</p>
        <p><strong>Status:</strong> <span className={sensor.status === 'active' ? 'status-active' : 'status-inactive'}>{sensor.status}</span></p>
        <p><strong>Room:</strong> {sensor.roomId || 'Not assigned'}</p>
      </div>

      {/* Display latest measurements */}
      {latestMeasurement && (
        <div className="measurement-display">
          <h4 className="measurement-title">📊 Latest Readings</h4>
          <div className="measurement-grid">
            {latestMeasurement.temperature !== undefined && (
              <div className="measurement-item">
                <span className="measurement-label">Temperature:</span>
                <span className="measurement-value">{latestMeasurement.temperature.toFixed(1)}°C</span>
              </div>
            )}
            {latestMeasurement.co2 !== undefined && (
              <div className="measurement-item">
                <span className="measurement-label">CO2:</span>
                <span className="measurement-value">{latestMeasurement.co2.toFixed(0)} ppm</span>
              </div>
            )}
            {latestMeasurement.humidity !== undefined && (
              <div className="measurement-item">
                <span className="measurement-label">Humidity:</span>
                <span className="measurement-value">{latestMeasurement.humidity.toFixed(1)}%</span>
              </div>
            )}
            {latestMeasurement.air_quality_index !== undefined && (
              <div className="measurement-item">
                <span className="measurement-label">AQI:</span>
                <span className="measurement-value">{latestMeasurement.air_quality_index.toFixed(0)}</span>
              </div>
            )}
            {latestMeasurement.power_consumption !== undefined && (
              <div className="measurement-item">
                <span className="measurement-label">Power:</span>
                <span className="measurement-value">{latestMeasurement.power_consumption.toFixed(2)}W</span>
              </div>
            )}
            {latestMeasurement.time && (
              <div className="measurement-time">
                <span className="time-label">Last updated:</span>
                <span className="time-value">{new Date(latestMeasurement.time).toLocaleString()}</span>
              </div>
            )}
          </div>
        </div>
      )}

      {!latestMeasurement && (
        <div className="no-measurement">
          <p>⏳ Waiting for measurements...</p>
        </div>
      )}

      {/* Display existing rule */}
      {existingRule && !isEditing && (
        <div className="rule-display">
          <h4 className="rule-title">📋 Current Rule</h4>
          <div className="rule-info">
            <p><strong>Type:</strong> {existingRule.type}</p>
            <p><strong>Condition:</strong> {existingRule.type} {existingRule.op} {existingRule.threshold}</p>
            <p><strong>Cooldown:</strong> {existingRule.cooldownSeconds} seconds</p>
            <p><strong>Status:</strong> 
              <span className={existingRule.enabled ? 'rule-enabled' : 'rule-disabled'}>
                {existingRule.enabled ? ' ✅ Enabled' : ' ❌ Disabled'}
              </span>
            </p>
          </div>
        </div>
      )}

      {/* Show message if no rule exists */}
      {!existingRule && !isEditing && (
        <div className="no-rule">
          <p>⚠️ No rule configured for this sensor</p>
        </div>
      )}

      {!isEditing ? (
        <div className="button-group">
          <button className="btn-edit" onClick={() => setIsEditing(true)}>
            {existingRule ? 'Edit Rule' : 'Create Rule'}
          </button>
          <button 
        className="btn-delete" 
        onClick={handleDelete} 
        disabled={!existingRule}
        title={!existingRule ? "No rule to delete" : "Delete rule"}
                  >
              Delete Rule
        </button>
        </div>
      ) : (
        <div className="edit-form">
          <h4 className="form-title">{existingRule ? 'Edit Rule' : 'Create New Rule'}</h4>

          <div className="form-group">
            <label className="form-label">Type:</label>
            <select
              name="type"
              value={ruleData.type}
              onChange={handleInputChange}
              className="form-select"
            >
              <option value="temperature">Temperature</option>
              <option value="co2">CO2</option>
              <option value="humidity">Humidity</option>
              <option value="air_quality_index">Air Quality Index</option>
              <option value="power_consumption">Power Consumption</option>
              <option value="motion_detected">Motion Detected</option>
            </select>
          </div>

          <div className="form-group">
            <label className="form-label">Operator:</label>
            <select
              name="op"
              value={ruleData.op}
              onChange={handleInputChange}
              className="form-select"
            >
              <option value=">">Greater than (&gt;)</option>
              <option value=">=">Greater or equal (&gt;=)</option>
              <option value="<">Less than (&lt;)</option>
              <option value="<=">Less or equal (&lt;=)</option>
              <option value="==">Equal (==)</option>
              <option value="!=">Not equal (!=)</option>
            </select>
          </div>

          <div className="form-group">
            <label className="form-label">Threshold:</label>
            <input
              type="number"
              name="threshold"
              value={ruleData.threshold}
              onChange={handleInputChange}
              step="0.1"
              className="form-input"
              placeholder="e.g., 28"
            />
          </div>

          <div className="form-group">
            <label className="form-label">Cooldown (seconds):</label>
            <input
              type="number"
              name="cooldownSeconds"
              value={ruleData.cooldownSeconds}
              onChange={handleInputChange}
              step="1"
              className="form-input"
              placeholder="e.g., 300"
            />
            <small className="help-text">Time between alerts for the same rule</small>
          </div>

          <div className="form-group">
            <label className="checkbox-label">
              <input
                type="checkbox"
                name="enabled"
                checked={ruleData.enabled}
                onChange={handleInputChange}
                className="form-checkbox"
              />
              Rule Enabled
            </label>
          </div>

          <div className="button-group">
            <button className="btn-save" onClick={handleSaveRule}>
              {existingRule ? 'Update Rule' : 'Create Rule'}
            </button>
            <button className="btn-cancel" onClick={handleCancel}>
              Cancel
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default SensorsCard;