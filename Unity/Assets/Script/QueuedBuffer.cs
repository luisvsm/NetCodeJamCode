public class QueuedBuffer
{
    public byte[] buffer = new byte[NetworkClient.MaxMessageSize];
    public int size = 0;
}