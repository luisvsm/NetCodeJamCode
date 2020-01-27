using System;

public class NetworkMessage
{
    public enum MessageType
    {
        InvalidMessage = 0,
        // Game setup
        PlayerJoined = 1,
        PlayerLeft = 2,
        PlayerSeeds = 3,
        FindGame = 4,

        // Gameplay
        InputLeft = 10,
        InputRight = 11,
        InputRotate = 12,
        InputDown = 13,
    }

    public static MessageType ParseMessage(byte[] inputBuffer, int messageSize)
    {
        if (messageSize >= 2)
            return (MessageType)BitConverter.ToUInt16(inputBuffer, 0);
        else
            return MessageType.InvalidMessage;
    }

    public static int[] ParseSeedData(byte[] inputBuffer, int messageSize)
    {
        if (messageSize == 10)
        {
            return new int[] {
                BitConverter.ToInt32(inputBuffer, 2),
                BitConverter.ToInt32(inputBuffer, 6)
            };
        }
        else
        {
            return new int[0];
        }
    }
}