using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/// <summary>
/// Arduino UDP Receiver - Buttons + Sliders
/// Empfängt Events vom Arduino via Python Bridge
/// </summary>
public class ArduinoUDPReceiver : MonoBehaviour
{
    [Header("UDP Settings")]
    [Tooltip("Port muss mit Python Script übereinstimmen")]
    public int udpPort = 5006;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // UDP Client
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private bool isRunning = false;

    // Event fuer empfangene Button Events
    public delegate void OnButtonEvent(int buttonId, string action);
    public event OnButtonEvent ButtonPressed;
    public event OnButtonEvent ButtonReleased;

    // Event fuer empfangene Slider Events
    public delegate void OnSliderEvent(int sliderId, float value);
    public event OnSliderEvent SliderChanged;

    void Start()
    {
        try
        {
            udpClient = new UdpClient(udpPort);
            remoteEndPoint = new IPEndPoint(IPAddress.Any, udpPort);
            isRunning = true;
            udpClient.BeginReceive(ReceiveCallback, null);

            Debug.Log($"<color=green>Arduino UDP Receiver gestartet auf Port {udpPort}</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"Kann UDP Socket nicht starten: {e.Message}");
        }
    }

    void OnDestroy()
    {
        isRunning = false;

        if (udpClient != null)
        {
            try
            {
                udpClient.Close();
            }
            catch
            {
                // Ignore dispose errors
            }
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        if (!isRunning)
            return;

        try
        {
            byte[] data = udpClient.EndReceive(ar, ref remoteEndPoint);
            string message = Encoding.UTF8.GetString(data);

            // Parse Message im Main Thread
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                HandleEvent(message);
            });

            // Weiter empfangen (nur wenn noch aktiv)
            if (isRunning && udpClient != null)
            {
                udpClient.BeginReceive(ReceiveCallback, null);
            }
        }
        catch (ObjectDisposedException)
        {
            // Normal beim Beenden - ignorieren
        }
        catch (Exception e)
        {
            if (isRunning && udpClient != null)
            {
                Debug.LogError($"UDP Receive Fehler: {e.Message}");

                try
                {
                    udpClient.BeginReceive(ReceiveCallback, null);
                }
                catch
                {
                    // Ignore if already disposed
                }
            }
        }
    }

    private void HandleEvent(string jsonString)
    {
        try
        {
            // Parse Event Type
            var msg = JsonUtility.FromJson<EventTypeCheck>(jsonString);

            if (msg.type == "button_event")
            {
                HandleButtonEvent(jsonString);
            }
            else if (msg.type == "slider_event")
            {
                HandleSliderEvent(jsonString);
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"Unbekannter Event Type: {msg.type}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Fehler beim Parsen von Event: {e.Message}");
        }
    }

    private void HandleButtonEvent(string jsonString)
    {
        try
        {
            ButtonEventMessage msg = JsonUtility.FromJson<ButtonEventMessage>(jsonString);

            if (msg != null)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"Button {msg.button_id}: {msg.action}");
                }

                if (msg.action == "pressed")
                {
                    ButtonPressed?.Invoke(msg.button_id, msg.action);
                }
                else if (msg.action == "released")
                {
                    ButtonReleased?.Invoke(msg.button_id, msg.action);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Fehler beim Parsen von Button Event: {e.Message}");
        }
    }

    private void HandleSliderEvent(string jsonString)
    {
        try
        {
            SliderEventMessage msg = JsonUtility.FromJson<SliderEventMessage>(jsonString);

            if (msg != null)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"Slider {msg.slider_id}: {msg.value:F3}");
                }

                SliderChanged?.Invoke(msg.slider_id, msg.value);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Fehler beim Parsen von Slider Event: {e.Message}");
        }
    }

    [Serializable]
    private class EventTypeCheck
    {
        public string type;
    }
}