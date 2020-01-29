using UnityEngine;
using System;
using System.Threading;
using System.Collections;

public class LocalServer : MonoBehaviour
{

    private ServerMain localServer;
    private Thread localServerThread;

    // Singleton structure, requires a NetworkClient in the scene to work
    private static LocalServer _instance;
    public static LocalServer instance
    {
        get
        {
            // Find the NetworkClient instance and call init
            if (_instance == null)
            {
                _instance = GameObject.Find("LocalServer").GetComponent<LocalServer>();
                if (_instance == null)
                {
                    Debug.LogError("Can't find LocalServer GameObject. Please check that it exists in the scene.");
                }
            }

            return _instance;
        }
    }

    public byte[] GetLocalHostToken()
    {
        return localServer.GetHostToken();
    }

    private void OnApplicationQuit()
    {
        StopServer();
    }

    public void StopServer()
    {

        if (localServerThread != null)
        {
            localServerThread.Abort();
            localServerThread = null;
        }
        if (localServer != null)
        {
            localServer.Stop();
            localServer = null;
        }
    }

    public void StartServer()
    {
        if (localServer == null)
        {
            localServer = new ServerMain();

            localServerThread = new Thread(new ThreadStart(StartServerWithTryCatch));
            localServerThread.Start();

            Debug.Log("[LocalServer] Started Server.");
        }
    }

    private void StartServerWithTryCatch()
    {
        try
        {
            localServer.Start("local");
        }
        catch (ThreadAbortException)
        {
            Debug.Log("Aborted local server");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            //handle
        }
    }
}