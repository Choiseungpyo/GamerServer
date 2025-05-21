using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class TcpManager : Singleton<TcpManager>, IListener
{
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;

    private int id;

    public int Id
    {
        get { return id; }
        set { id = value; }
    }

    void Start()
    {
        id = 0;

        EventManager.Instance.AddListener(EVENT_TYPE.SEND_PLAYER_MOVEMENT, this);
        EventManager.Instance.AddListener(EVENT_TYPE.SEND_USER_NICKNAME, this);
        ConnectToServer("127.0.0.1", 8080);
    }

    void ConnectToServer(string ip, int port)
    {
        try
        {
            client = new TcpClient(ip, port);
            stream = client.GetStream();
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.Start();
            AppendMessage("������ ����Ǿ����ϴ�.");
            
        }
        catch (Exception e)
        {
            AppendMessage("���� ����: " + e.Message);
        }
    }

    void ReceiveLoop()
    {
        //try
        {
            while (client.Connected)
            {
                // 1�ܰ�: ���(���� + Ÿ��) ���� ����
                int headerSize = Marshal.SizeOf(typeof(UInt32)) + Marshal.SizeOf(typeof(int));
                byte[] headerBytes = new byte[headerSize];
                int read = 0;
                while (read < headerSize)
                {
                    int r = stream.Read(headerBytes, read, headerSize - read);
                    if (r <= 0) throw new Exception("���� ���� ����");
                    read += r;
                }

                // 2�ܰ�: ��� �Ľ�
                UInt32 length = BitConverter.ToUInt32(headerBytes, 0);
                int type = BitConverter.ToInt32(headerBytes, 4);

                // 3�ܰ�: ��ü ��Ŷ ���� - �����ŭ ������ ����
                int bodySize = (int)(length - headerSize);
                if (bodySize > 0)
                {
                    Debug.Log(bodySize);
                    byte[] bodyBytes = new byte[bodySize];
                    int totalRead = 0;
                    while (totalRead < bodySize)
                    {
                        int r = stream.Read(bodyBytes, totalRead, bodySize - totalRead);
                        if (r <= 0) throw new Exception("���� ���� ����");
                        totalRead += r;
                    }

                    // 4�ܰ�: headerBytes + bodyBytes �� full packet���� ����
                    byte[] fullPacket = new byte[headerSize + bodySize];
                    Buffer.BlockCopy(headerBytes, 0, fullPacket, 0, headerSize);
                    Buffer.BlockCopy(bodyBytes, 0, fullPacket, headerSize, bodySize);

                    // 5�ܰ�: ��Ŷ Ÿ�Կ� ���� ������ȭ
                    switch (type)
                    {
                        case (int)PTYPE.S_ID:
                            {
                                PACKET_S_ID packet = BytesToStruct<PACKET_S_ID>(fullPacket);
                                id = packet.Id;
                            }
                            break;

                        case (int)PTYPE.S_PLAYER_SPAWN:
                            {
                                PACKET_S_SPAWN packet = BytesToStruct<PACKET_S_SPAWN>(fullPacket);
                                EventManager.Instance.PostNotification(EVENT_TYPE.ADD_PLAYER, this, packet);
                            }
                            break;

                        case (int)PTYPE.S_PLAYER_MOVE:
                            {
                                PACKET_S_MOVE packet = BytesToStruct<PACKET_S_MOVE>(fullPacket);
                                EventManager.Instance.PostNotification(EVENT_TYPE.APPLY_PLAYER_MOVEMENT, this, packet);
                            }
                            break;

                    }
                }
            }
        }
        //catch (Exception ex)
        {
            //AppendMessage("���� �� ����: " + ex.Message);
        }
    }

    public void RegisterJop(Action job)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(job);
    }

    void AppendMessage(string message)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            Debug.Log(message);
            //chatDisplay.text += message + "\n";
        });
    }

    byte[] StructToBytes<T>(T obj) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        byte[] buffer = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        try
        {
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, buffer, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return buffer;
    }

    T BytesToStruct<T>(byte[] bytes) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        if (bytes.Length < size)
            throw new ArgumentException("�Է� ����Ʈ �迭�� ����ü ũ�⺸�� �۽��ϴ�.");

        IntPtr ptr = Marshal.AllocHGlobal(size);

        try
        {
            Marshal.Copy(bytes, 0, ptr, size);
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null)
        {
            receiveThread.Join(); // �����尡 ���� ������ ��ٸ�
        }
        //if (receiveThread != null) receiveThread.Abort();
        if (stream != null) stream.Close();
        if (client != null) client.Close();
    }


    public byte GetByteFromBoolArray(bool[] data)
    {
        byte directionsByte = 0;  // byte ���� (4���� bool ���� ���� ����)

        // �� ������ bool ���� byte�� �� ��Ʈ�� ����
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i])
            {
                directionsByte |= (byte)(1 << i);  // �ش� ��Ʈ�� 1�� ����
            }
        }

        return directionsByte;
    }

    public void OnEvent(EVENT_TYPE EventType, Component Sender, object Param = null)
    {
        switch(EventType)
        {
            case EVENT_TYPE.SEND_PLAYER_MOVEMENT:
                {
                    byte data = GetByteFromBoolArray((bool[])Param);
                    Debug.Log("�̺�Ʈ ���� : PlayerMovement");
                    PACKET_C_MOVE packet = new PACKET_C_MOVE
                    {
                        Type = (int)PTYPE.C_PLAYER_MOVE,
                        Directions = data
                    };
                    packet.Length = (uint)Marshal.SizeOf(typeof(PACKET_C_MOVE));
                    byte[] bytes = StructToBytes(packet);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }
                break;
            case EVENT_TYPE.SEND_USER_NICKNAME:
                {
                    string data = (string)Param;

                }
                break;

            default:
                break;
        }
    }
}
