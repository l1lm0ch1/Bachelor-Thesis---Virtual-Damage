using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// UDP Receiver für Tracking-Daten von Python
/// Empfängt JSON Messages auf Port 5005
/// </summary>
public class UDPReceiver : MonoBehaviour
{
    [Header("UDP Configuration")]
    [Tooltip("Port auf dem Python sendet (muss mit Python übereinstimmen!)")]
    public int port = 5005;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // UDP Socket
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;

    // Event für empfangene Nachrichten
    public delegate void OnMessageReceived(string messageType, string jsonData);
    public event OnMessageReceived MessageReceived;

    // Stats
    private int messagesReceived = 0;
    private float lastMessageTime = 0f;

    void Start()
    {
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

    /// <summary>
    /// Startet UDP Listener in separatem Thread
    /// </summary>
    private void StartUDPListener()
    {
        try
        {
            udpClient = new UdpClient(port);
            isRunning = true;

            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log($"<color=green>✓ UDP Listener gestartet auf Port {port}</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"Fehler beim Starten des UDP Listeners: {e.Message}");
        }
    }

    /// <summary>
    /// Stoppt UDP Listener
    /// </summary>
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

        Debug.Log("<color=yellow>UDP Listener gestoppt</color>");
    }

    /// <summary>
    /// Empfängt Daten im separaten Thread
    /// </summary>
    private void ReceiveData()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEndPoint);

                // Dekodiere JSON String
                string jsonString = Encoding.UTF8.GetString(data);

                // Parse Message Type
                MessageData messageData = JsonUtility.FromJson<MessageData>(jsonString);

                // Event auslösen (auf Main Thread via Update)
                lock (this)
                {
                    messagesReceived++;
                    lastMessageTime = Time.time;
                }

                // Event auf Main Thread via Dispatcher
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    MessageReceived?.Invoke(messageData.type, jsonString);

                    if (showDebugLogs && messagesReceived % 30 == 0)
                    {
                        Debug.Log($"📡 UDP: {messagesReceived} Messages empfangen");
                    }
                });
            }
            catch (ThreadAbortException)
            {
                // Thread wird beendet, normal
                break;
            }
            catch (Exception e)
            {
                if (isRunning)
                {
                    Debug.LogWarning($"UDP Receive Error: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Zeigt Statistiken in Inspector
    /// </summary>
    void OnGUI()
    {
        if (showDebugLogs)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label($"UDP Port: {port}");
            GUILayout.Label($"Messages: {messagesReceived}");
            GUILayout.Label($"Last Message: {(Time.time - lastMessageTime < 1f ? "AKTIV" : "INAKTIV")}");
            GUILayout.EndArea();
        }
    }
}

/// <summary>
/// Basis Message Struktur (nur für Type)
/// </summary>
[Serializable]
public class MessageData
{
    public string type;
}

/// <summary>
/// Main Thread Dispatcher für Thread-Safety
/// Singleton Pattern
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance = null;
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            GameObject obj = new GameObject("MainThreadDispatcher");
            _instance = obj.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(obj);
        }
        return _instance;
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
}