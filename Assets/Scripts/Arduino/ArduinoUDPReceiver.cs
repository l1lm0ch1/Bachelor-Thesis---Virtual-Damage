using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// UDP Receiver für Arduino Button Events
/// Port 5006 (separat von Tracking Port 5005)
/// </summary>
public class ArduinoUDPReceiver : MonoBehaviour
{
    [Header("UDP Configuration")]
    [Tooltip("Port für Arduino Daten (verschieden von Tracking!)")]
    public int port = 5006;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // UDP Socket
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;

    // Event für empfangene Button Events
    public delegate void OnButtonEvent(int buttonId, string action);
    public event OnButtonEvent ButtonPressed;
    public event OnButtonEvent ButtonReleased;

    // Stats
    private int messagesReceived = 0;

    void Start()
    {
        // WICHTIG: MainThreadDispatcher initialisieren (falls nicht schon durch UDPReceiver)
        if (FindFirstObjectByType<UnityMainThreadDispatcher>() == null)
        {
            UnityMainThreadDispatcher.Instance();
        }

        StartUDPListener();
    }

    void OnDestroy()
    {
        StopUDPListener();
    }

    void OnApplicationQuit()
    {
        StopUDPListener();
    }

    private void StartUDPListener()
    {
        try
        {
            udpClient = new UdpClient(port);
            isRunning = true;

            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log($"<color=green>Arduino UDP Listener gestartet auf Port {port}</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"Fehler beim Starten des Arduino UDP Listeners: {e.Message}");
        }
    }

    private void StopUDPListener()
    {
        isRunning = false;

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }

        if (udpClient != null)
        {
            udpClient.Close();
        }

        Debug.Log("Arduino UDP Listener gestoppt");
    }

    private void ReceiveData()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEndPoint);

                string jsonString = Encoding.UTF8.GetString(data);

                lock (this)
                {
                    messagesReceived++;
                }

                // Parse und Event auslösen auf Main Thread
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    HandleButtonEvent(jsonString);
                });
            }
            catch (ThreadAbortException)
            {
                break;
            }
            catch (Exception e)
            {
                if (isRunning)
                {
                    Debug.LogWarning($"Arduino UDP Receive Error: {e.Message}");
                }
            }
        }
    }

    private void HandleButtonEvent(string jsonString)
    {
        try
        {
            ButtonEventMessage msg = JsonUtility.FromJson<ButtonEventMessage>(jsonString);

            if (msg != null && msg.type == "button_event")
            {
                if (showDebugLogs)
                {
                    Debug.Log($"Button {msg.button_id}: {msg.action}");
                }

                // Events auslösen
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

    void OnGUI()
    {
        if (showDebugLogs)
        {
            GUILayout.BeginArea(new Rect(10, 220, 300, 80));
            GUILayout.Label($"<b>Arduino UDP</b>");
            GUILayout.Label($"Port: {port}");
            GUILayout.Label($"Messages: {messagesReceived}");
            GUILayout.Label($"Status: {(isRunning ? "RUNNING" : "STOPPED")}");
            GUILayout.EndArea();
        }
    }
}