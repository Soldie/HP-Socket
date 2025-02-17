﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Text;
using System.IO;

namespace HPSocketCS
{
    public class TcpAgentEvent
    {
        public delegate HandleResult OnConnectEventHandler(IntPtr connId /* IntPtr pClient */);
        public delegate HandleResult OnSendEventHandler(IntPtr connId, byte[] bytes);
        public delegate HandleResult OnReceiveEventHandler(IntPtr connId, byte[] bytes);
        public delegate HandleResult OnPointerDataReceiveEventHandler(IntPtr connId, IntPtr pData, int length);
        public delegate HandleResult OnCloseEventHandler(IntPtr connId, SocketOperation enOperation, int errorCode);
        public delegate HandleResult OnShutdownEventHandler();
        public delegate HandleResult OnPrepareConnectEventHandler(IntPtr connId /* IntPtr pClient */, IntPtr socket);
        public delegate HandleResult OnHandShakeEventHandler(IntPtr connId);
    }

    public class TcpAgent<T> : TcpAgent
    {
        public new T GetExtra(IntPtr connId)
        {
            return base.GetExtra<T>(connId);
        }

        public bool SetExtra(IntPtr connId, T obj)
        {
            return base.SetExtra(connId, obj);
        }
    }

    public class TcpAgent : ConnectionExtra
    {
        protected IntPtr _pAgent = IntPtr.Zero;

        protected IntPtr pAgent
        {
            get
            {
                //if (_pClient == IntPtr.Zero)
                //{
                //    throw new Exception("pClient == 0");
                //}

                return _pAgent;
            }

            set
            {
                _pAgent = value;
            }
        }


        protected IntPtr pListener = IntPtr.Zero;

        /// <summary>
        /// 连接事件
        /// </summary>
        public event TcpAgentEvent.OnConnectEventHandler OnConnect;
        /// <summary>
        /// 数据发送事件
        /// </summary>
        public event TcpAgentEvent.OnSendEventHandler OnSend;
        /// <summary>
        /// 准备连接事件
        /// </summary>
        public event TcpAgentEvent.OnPrepareConnectEventHandler OnPrepareConnect;
        /// <summary>
        /// 数据到达事件
        /// </summary>
        public event TcpAgentEvent.OnReceiveEventHandler OnReceive;
        /// <summary>
        /// 数据到达事件(指针数据)
        /// </summary>
        public event TcpAgentEvent.OnPointerDataReceiveEventHandler OnPointerDataReceive;
        /// <summary>
        /// 连接关闭事件
        /// </summary>
        public event TcpAgentEvent.OnCloseEventHandler OnClose;
        /// <summary>
        /// 服务关闭事件
        /// </summary>
        public event TcpAgentEvent.OnShutdownEventHandler OnShutdown;

        public event TcpAgentEvent.OnHandShakeEventHandler OnHandShake;


        protected bool IsCreate = false;

        public TcpAgent()
        {
            CreateListener();
        }

        ~TcpAgent()
        {
            Destroy();
        }

        public IntPtr Sender
        {
            get
            {
                return pAgent;
            }
        }

        /// <summary>
        /// 创建socket监听&服务组件
        /// </summary>
        /// <returns></returns>
        protected virtual bool CreateListener()
        {
            if (IsCreate == true || pListener != IntPtr.Zero || pAgent != IntPtr.Zero)
            {
                return false;
            }

            pListener = Sdk.Create_HP_TcpAgentListener();
            if (pListener == IntPtr.Zero)
            {
                return false;
            }

            pAgent = Sdk.Create_HP_TcpAgent(pListener);
            if (pAgent == IntPtr.Zero)
            {
                return false;
            }

            IsCreate = true;

            return true;
        }

        /// <summary>
        /// 终止服务并释放资源
        /// </summary>
        public virtual void Destroy()
        {
            Stop();

            if (pAgent != IntPtr.Zero)
            {
                Sdk.Destroy_HP_TcpAgent(pAgent);
                pAgent = IntPtr.Zero;
            }
            if (pListener != IntPtr.Zero)
            {
                Sdk.Destroy_HP_TcpAgentListener(pListener);
                pListener = IntPtr.Zero;
            }

            IsCreate = false;
        }

        /// <summary>
        /// 启动通讯组件
        /// 启动完成后可开始连接远程服务器
        /// </summary>
        /// <param name="address">绑定地址</param>
        /// <param name="async">是否异步</param>
        /// <returns></returns>
        public bool Start(string address, bool async = true)
        {
            if (string.IsNullOrEmpty(address) == true)
            {
                throw new Exception("address is null");
            }

            if (IsCreate == false)
            {
                return false;
            }

            if (pAgent == IntPtr.Zero)
            {
                return false;
            }

            SetCallback();

            return Sdk.HP_Agent_Start(pAgent, address, async);
        }

        /// <summary>
        /// 停止通讯组件
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            if (pAgent == IntPtr.Zero)
            {
                return false;
            }
            return Sdk.HP_Agent_Stop(pAgent);
        }

        public IntPtr Connect(string address, ushort port)
        {
            IntPtr connId = IntPtr.Zero;
            Sdk.HP_Agent_Connect(pAgent, address, port, ref connId);
            return connId;
        }

        public IntPtr Connect(EndPoint endpoint)
        {
            IntPtr connId = IntPtr.Zero;
            Sdk.HP_Agent_Connect(pAgent, endpoint.Address, endpoint.Port, ref connId);
            return connId;
        }

        /// <summary>
        /// 连接服务器，连接成功后 IAgentListener 会接收到 OnConnect() 事件
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="connId"></param>
        /// <returns></returns>
        public bool Connect(string address, ushort port, ref IntPtr connId)
        {
            return Sdk.HP_Agent_Connect(pAgent, address, port, ref connId);
        }

        /// <summary>
        /// 连接服务器，连接成功后 IAgentListener 会接收到 OnConnect() 事件
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="connId"></param>
        /// <param name="usLocalPort">本地端口</param>
        /// <returns></returns>
        public bool Connect(string address, ushort port, ref IntPtr connId, ushort usLocalPort)
        {
            return Sdk.HP_Agent_ConnectWithLocalPort(pAgent, address, port, ref connId, usLocalPort);
        }

        /// <summary>
        /// 连接服务器，连接成功后 IAgentListener 会接收到 OnConnect() 事件
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="connId"></param>
        /// <param name="localAddress">本地地址（默认：nullptr，使用 Start() 方法中绑定的地址）</param>
        /// <returns></returns>
        public bool Connect(string address, ushort port, ref IntPtr connId, string localAddress)
        {
            return Sdk.HP_Agent_ConnectWithLocalAddress(pAgent, address, port, ref connId, localAddress);
        }

        /// <summary>
        /// 连接服务器，连接成功后 IAgentListener 会接收到 OnConnect() 事件
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="connId"></param>
        /// <param name="localAddress">本地地址（默认：nullptr，使用 Start() 方法中绑定的地址）</param>
        /// <param name="usLocalPort">本地端口</param>
        /// <returns></returns>
        public bool Connect(string address, ushort port, ref IntPtr connId, string localAddress, ushort usLocalPort)
        {
            return Sdk.HP_Agent_ConnectWithExtraAndLocalAddressPort(pAgent, address, port, ref connId, IntPtr.Zero, usLocalPort, localAddress);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="bytes"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public bool Send(IntPtr connId, byte[] bytes, int size)
        {
            return Sdk.HP_Agent_Send(pAgent, connId, bytes, size);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="bufferPtr"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public bool Send<T>(IntPtr connId, T obj)
        {
            byte[] buffer = StructureToByte<T>(obj);
            return Send(connId, buffer, buffer.Length);
        }

        /// <summary>
        /// 序列化对象后发送数据,序列化对象所属类必须标记[Serializable]
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="bufferPtr"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public bool SendBySerializable(IntPtr connId, object obj)
        {
            byte[] buffer = ObjectToBytes(obj);
            return Send(connId, buffer, buffer.Length);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="bufferPtr"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public bool Send(IntPtr connId, IntPtr bufferPtr, int size)
        {
            return Sdk.HP_Agent_Send(pAgent, connId, bufferPtr, size);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="bytes"></param>
        /// <param name="offset">针对bytes的偏移</param>
        /// <param name="size">发多大</param>
        /// <returns></returns>
        public bool Send(IntPtr connId, byte[] bytes, int offset, int size)
        {
            return Sdk.HP_Agent_SendPart(pAgent, connId, bytes, size, offset);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="bufferPtr"></param>
        /// <param name="offset">针对bufferPtr的偏移</param>
        /// <param name="size">发多大</param>
        /// <returns></returns>
        public bool Send(IntPtr connId, IntPtr bufferPtr, int offset, int size)
        {
            return Sdk.HP_Agent_SendPart(pAgent, connId, bufferPtr, size, offset);
        }

        /// <summary>
        /// 发送多组数据
        /// 向指定连接发送多组数据
        /// TCP - 顺序发送所有数据包
        /// </summary>
        /// <param name="connId">连接 ID</param>
        /// <param name="pBuffers">发送缓冲区数组</param>
        /// <param name="iCount">发送缓冲区数目</param>
        /// <returns>TRUE.成功,FALSE.失败，可通过 SYSGetLastError() 获取 Windows 错误代码</returns>
        public bool SendPackets(IntPtr connId, WSABUF[] pBuffers, int count)
        {
            return Sdk.HP_Agent_SendPackets(pAgent, connId, pBuffers, count);
        }

        /// <summary>
        /// 发送多组数据
        /// 向指定连接发送多组数据
        /// TCP - 顺序发送所有数据包
        /// </summary>
        /// <param name="connId">连接 ID</param>
        /// <param name="pBuffers">发送缓冲区数组</param>
        /// <param name="iCount">发送缓冲区数目</param>
        /// <returns>TRUE.成功,FALSE.失败，可通过 SYSGetLastError() 获取 Windows 错误代码</returns>
        public bool SendPackets<T>(IntPtr connId, T[] objects)
        {
            bool ret = false;

            WSABUF[] buffer = new WSABUF[objects.Length];
            IntPtr[] ptrs = new IntPtr[buffer.Length];
            try
            {

                for (int i = 0; i < objects.Length; i++)
                {
                    buffer[i].Length = Marshal.SizeOf(typeof(T));

                    ptrs[i] = Marshal.AllocHGlobal(buffer[i].Length);
                    Marshal.StructureToPtr(objects[i], ptrs[i], true);

                    buffer[i].Buffer = ptrs[i];
                }
                ret = SendPackets(connId, buffer, buffer.Length);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                for (int i = 0; i < ptrs.Length; i++)
                {
                    if (ptrs[i] != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptrs[i]);
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// 名称：发送小文件
        /// 描述：向指定连接发送 4096 KB 以下的小文件
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="filePath">文件路径</param>
        /// <param name="head">头部附加数据</param>
        /// <param name="tail">尾部附加数据</param>
        /// <returns>TRUE.成功,FALSE.失败，可通过 SYSGetLastError() 获取 Windows 错误代码</returns>
        public bool SendSmallFile(IntPtr connId, string filePath, ref WSABUF head, ref WSABUF tail)
        {
            return Sdk.HP_TcpAgent_SendSmallFile(pAgent, connId, filePath, ref head, ref tail);
        }

        /// <summary>
        /// 名称：发送小文件
        /// 描述：向指定连接发送 4096 KB 以下的小文件
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="filePath">文件路径</param>
        /// <param name="head">头部附加数据,可以为null</param>
        /// <param name="tail">尾部附加数据,可以为null</param>
        /// <returns>TRUE.成功,FALSE.失败，可通过 SYSGetLastError() 获取 Windows 错误代码</returns>
        public bool SendSmallFile(IntPtr connId, string filePath, byte[] head, byte[] tail)
        {
            IntPtr pHead = IntPtr.Zero;
            IntPtr pTail = IntPtr.Zero;
            WSABUF wsaHead = new WSABUF() { Length = 0, Buffer = pHead };
            WSABUF wsatail = new WSABUF() { Length = 0, Buffer = pTail };
            if (head != null)
            {
                wsaHead.Length = head.Length;
                wsaHead.Buffer = Marshal.UnsafeAddrOfPinnedArrayElement(head, 0);
            }

            if (tail != null)
            {
                wsaHead.Length = tail.Length;
                wsaHead.Buffer = Marshal.UnsafeAddrOfPinnedArrayElement(tail, 0);
            }

            return SendSmallFile(connId, filePath, ref wsaHead, ref wsatail);
        }

        /// <summary>
        /// 名称：发送小文件
        /// 描述：向指定连接发送 4096 KB 以下的小文件
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="filePath">文件路径</param>
        /// <param name="head">头部附加数据,可以为null</param>
        /// <param name="tail">尾部附加数据,可以为null</param>
        /// <returns>TRUE.成功,FALSE.失败，可通过 SYSGetLastError() 获取 Windows 错误代码</returns>
        public bool SendSmallFile<T1, T2>(IntPtr connId, string filePath, T1 head, T2 tail)
        {

            byte[] headBuffer = null;
            if (head != null)
            {
                headBuffer = StructureToByte<T1>(head);
            }

            byte[] tailBuffer = null;
            if (tail != null)
            {
                tailBuffer = StructureToByte<T2>(tail);
            }
            return SendSmallFile(connId, filePath, headBuffer, tailBuffer);
        }

        /// <summary>
        /// 断开某个连接
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="force">强制</param>
        /// <returns></returns>
        public bool Disconnect(IntPtr connId, bool force = true)
        {
            return Sdk.HP_Agent_Disconnect(pAgent, connId, force);
        }

        /// <summary>
        /// 断开超过指定时间的连接
        /// </summary>
        /// <param name="period">毫秒</param>
        /// <param name="force">强制</param>
        /// <returns></returns>
        public bool DisconnectLongConnections(uint period, bool force = true)
        {
            return Sdk.HP_Agent_DisconnectLongConnections(pAgent, period, force);
        }


        /// <summary>
        /// 断开超过指定时长的静默连接
        /// </summary>
        /// <param name="period">毫秒</param>
        /// <param name="force">强制</param>
        /// <returns></returns>
        public bool DisconnectSilenceConnections(uint period, bool force = true)
        {
            return Sdk.HP_Agent_DisconnectSilenceConnections(pAgent, period, force);
        }



        /// <summary>
        /// 暂停接收
        /// </summary>
        /// <param name="connId"></param>
        /// <returns></returns>
        public bool PauseReceive(IntPtr connId)
        {
            return Sdk.HP_Agent_PauseReceive(pAgent, connId, true);
        }

        /// <summary>
        /// 唤醒接收
        /// </summary>
        /// <param name="connId"></param>
        /// <returns></returns>
        public bool ResumeReceive(IntPtr connId)
        {
            return Sdk.HP_Agent_PauseReceive(pAgent, connId, false);
        }

        /// <summary>
        /// 获取连接的接收状态
        /// </summary>
        /// <param name="connId"></param>
        /// <returns></returns>
        public ReceiveState GetReceiveState(IntPtr connId)
        {
            int state = -1;

            if (Sdk.HP_Agent_IsPauseReceive(pAgent, connId, ref state))
            {
                return (ReceiveState)state;
            }
            return ReceiveState.Unknown;
        }


        ///// <summary>
        ///// 设置连接的附加数据
        ///// </summary>
        ///// <param name="connId"></param>
        ///// <param name="obj"></param>
        ///// <returns></returns>
        //public bool SetConnectionExtra(IntPtr connId, object obj)
        //{

        //    IntPtr ptr = IntPtr.Zero;
        //    // 释放附加数据
        //    if (Sdk.HP_Agent_GetConnectionExtra(pAgent, connId, ref ptr) && ptr != IntPtr.Zero)
        //    {
        //        Marshal.FreeHGlobal(ptr);
        //        ptr = IntPtr.Zero;
        //    }

        //    if (obj != null)
        //    {
        //        // 设置附加数据
        //        ptr = Marshal.AllocHGlobal(Marshal.SizeOf(obj));
        //        Marshal.StructureToPtr(obj, ptr, false);
        //    }
        //    return Sdk.HP_Agent_SetConnectionExtra(pAgent, connId, ptr);
        //}

        ///// <summary>
        ///// 获取附加数据
        ///// 如设置的是个结构体/类对象,可以用 Type objA = (Type)Marshal.PtrToStructure(ptr, typeof(Type)) 获取
        ///// 其中Type是结构体/类名,ptr是该方法的传出值,在该方法返回为true的时候可用
        ///// </summary>
        ///// <param name="connId"></param>
        ///// <param name="ptr"></param>
        ///// <returns></returns>
        //[Obsolete("该非泛型方法已过期,推荐使用泛型方法: T GetConnectionExtra<T>(IntPtr connId)")]
        //public bool GetConnectionExtra(IntPtr connId, ref IntPtr ptr)
        //{
        //    return Sdk.HP_Agent_GetConnectionExtra(pAgent, connId, ref ptr) && ptr != IntPtr.Zero;
        //}

        ///// <summary>
        ///// 获取附加数据
        ///// 成功时返回对象,失败时返回T类型默认值,如:int=0, classA=null
        ///// </summary>
        ///// <param name="connId"></param>
        ///// <returns></returns>
        //public T GetConnectionExtra<T>(IntPtr connId)
        //{
        //    IntPtr ptr = IntPtr.Zero;
        //    T obj = default(T);
        //    if (Sdk.HP_Agent_GetConnectionExtra(pAgent, connId, ref ptr))
        //    {
        //        obj = (T)Marshal.PtrToStructure(ptr, typeof(T));
        //    }
        //    return obj;
        //}
        ///// <summary>
        ///// 移除连接中的附加数据, 同SetConnectionExtra(id, null)
        ///// </summary>
        ///// <param name="connId"></param>
        ///// <returns></returns>
        //public bool RemoveConnectionExtra(IntPtr connId)
        //{
        //    return SetConnectionExtra(connId, null);
        //}

        /// <summary>
        /// 获取错误码
        /// </summary>
        public SocketError ErrorCode
        {
            get
            {
                return Sdk.HP_Agent_GetLastError(pAgent);
            }
        }

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version
        {
            get
            {
                return Sdk.GetHPSocketVersion();
            }
        }

        /// <summary>
        /// 获取错误信息
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                IntPtr ptr = Sdk.HP_Agent_GetLastErrorDesc(pAgent);
                string desc = Marshal.PtrToStringAnsi(ptr);
                return desc;
            }
        }


        /// <summary>
        /// 获取连接中未发出数据的长度
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool GetPendingDataLength(IntPtr connId, ref int length)
        {
            return Sdk.HP_Agent_GetPendingDataLength(pAgent, connId, ref length);
        }

        // 是否启动
        public bool IsStarted
        {
            get
            {
                if (pAgent == IntPtr.Zero)
                {
                    return false;
                }
                return Sdk.HP_Agent_HasStarted(pAgent);
            }
        }

        /// <summary>
        /// 状态
        /// </summary>
        public ServiceState State
        {
            get
            {
                return Sdk.HP_Agent_GetState(pAgent);
            }
        }

        public uint ConnectionCount
        {
            get
            {
                return Sdk.HP_Agent_GetConnectionCount(pAgent);
            }
        }

        /// <summary>
        /// 检测是否为安全连接（SSL/HTTPS）
        /// </summary>
        public bool IsSecure
        {
            get
            {
                return Sdk.HP_Agent_IsSecure(pAgent);
            }
        }

        /// <summary>
        /// 获取所有连接,未获取到连接返回null
        /// </summary>
        /// <returns></returns>
        public IntPtr[] GetAllConnectionIDs()
        {
            IntPtr[] arr = null;
            do
            {
                uint count = ConnectionCount;
                if (count == 0)
                {
                    break;
                }
                arr = new IntPtr[count];
                if (Sdk.HP_Agent_GetAllConnectionIDs(pAgent, arr, ref count))
                {
                    if (arr.Length > count)
                    {
                        IntPtr[] newArr = new IntPtr[count];
                        Array.Copy(arr, newArr, count);
                        arr = newArr;
                    }
                    break;
                }
            } while (true);

            return arr;
        }

        /// <summary>
        /// 获取某个连接的本地地址信息
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool GetLocalAddress(IntPtr connId, ref string ip, ref ushort port)
        {
            int ipLength = 40;

            StringBuilder sb = new StringBuilder(ipLength);

            bool ret = Sdk.HP_Agent_GetLocalAddress(pAgent, connId, sb, ref ipLength, ref port);
            if (ret == true)
            {
                ip = sb.ToString();
            }
            return ret;
        }

        /// <summary>
        /// 获取某个连接的远程地址信息
        /// </summary>
        /// <returns></returns>
        public bool GetRemoteAddress(IntPtr connId, ref string ip, ref ushort port)
        {
            int ipLength = 40;

            StringBuilder sb = new StringBuilder(ipLength);

            bool ret = Sdk.HP_Agent_GetRemoteAddress(pAgent, connId, sb, ref ipLength, ref port);
            if (ret == true)
            {
                ip = sb.ToString();
            }
            return ret;
        }


        /// <summary>
        /// 获取某个连接的远程主机信息
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool GetRemoteHost(IntPtr connId, ref string host, ref ushort port)
        {
            int ipLength = 40;

            StringBuilder sb = new StringBuilder(ipLength);

            bool ret = Sdk.HP_Agent_GetRemoteHost(pAgent, connId, sb, ref ipLength, ref port);
            if (ret == true)
            {
                host = sb.ToString();
            }
            return ret;
        }

        /// <summary>
        /// 获取指定连接的连接时长（毫秒）
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        public bool GetConnectPeriod(IntPtr connId, ref uint period)
        {
            return Sdk.HP_Agent_GetConnectPeriod(pAgent, connId, ref period);
        }

        /// <summary>
        /// 获取某个连接静默时间（毫秒）
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        public bool GetSilencePeriod(IntPtr connId, ref uint period)
        {
            return Sdk.HP_Agent_GetSilencePeriod(pAgent, connId, ref period);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 获取或设置是否启用地址重用机制（默认：不启用）
        /// </summary>
        public bool IsReuseAddress
        {
            get
            {
                return Sdk.HP_TcpAgent_IsReuseAddress(pAgent);
            }
            set
            {
                Sdk.HP_TcpAgent_SetReuseAddress(pAgent, value);
            }
        }


        /// <summary>
        /// 设置最大连接数（组件会根据设置值预分配内存，因此需要根据实际情况设置，不宜过大）
        /// </summary>
        public uint MaxConnectionCount
        {
            get
            {
                return Sdk.HP_Agent_GetMaxConnectionCount(pAgent);
            }
            set
            {
                Sdk.HP_Agent_SetMaxConnectionCount(pAgent, value);
            }
        }

        /// <summary>
        /// 读取或设置工作线程数量（通常设置为 2 * CPU + 2）
        /// </summary>
        public uint WorkerThreadCount
        {
            get
            {
                return Sdk.HP_Agent_GetWorkerThreadCount(pAgent);
            }
            set
            {
                Sdk.HP_Agent_SetWorkerThreadCount(pAgent, value);
            }
        }

        /// <summary>
        /// 读取或设置通信数据缓冲区大小（根据平均通信数据包大小调整设置，通常设置为 1024 的倍数）
        /// </summary>
        public uint SocketBufferSize
        {
            get
            {
                return Sdk.HP_TcpAgent_GetSocketBufferSize(pAgent);
            }
            set
            {
                Sdk.HP_TcpAgent_SetSocketBufferSize(pAgent, value);
            }
        }

        /// <summary>
        /// 读取或设置 Socket 缓存对象锁定时间（毫秒，在锁定期间该 Socket 缓存对象不能被获取使用）
        /// </summary>
        public uint FreeSocketObjLockTime
        {
            get
            {
                return Sdk.HP_Agent_GetFreeSocketObjLockTime(pAgent);
            }
            set
            {
                Sdk.HP_Agent_SetFreeSocketObjLockTime(pAgent, value);
            }
        }

        /// <summary>
        /// 读取或设置 Socket 缓存池大小（通常设置为平均并发连接数量的 1/3 - 1/2）
        /// </summary>
        public uint FreeSocketObjPool
        {
            get
            {
                return Sdk.HP_Agent_GetFreeSocketObjPool(pAgent);
            }
            set
            {
                Sdk.HP_Agent_SetFreeSocketObjPool(pAgent, value);
            }
        }

        /// <summary>
        /// 读取或设置内存块缓存池大小（通常设置为 Socket 缓存池大小的 2 - 3 倍）
        /// </summary>
        public uint FreeBufferObjPool
        {
            get
            {
                return Sdk.HP_Agent_GetFreeBufferObjPool(pAgent);
            }
            set
            {
                Sdk.HP_Agent_SetFreeBufferObjPool(pAgent, value);
            }
        }

        /// <summary>
        /// 读取或设置 Socket 缓存池回收阀值（通常设置为 Socket 缓存池大小的 3 倍）
        /// </summary>
        public uint FreeSocketObjHold
        {
            get
            {
                return Sdk.HP_Agent_GetFreeSocketObjHold(pAgent);
            }
            set
            {
                Sdk.HP_Agent_SetFreeSocketObjHold(pAgent, value);
            }
        }

        /// <summary>
        /// 读取或设置内存块缓存池回收阀值（通常设置为内存块缓存池大小的 3 倍）
        /// </summary>
        public uint FreeBufferObjHold
        {
            get
            {
                return Sdk.HP_Agent_GetFreeBufferObjHold(pAgent);
            }
            set
            {
                Sdk.HP_Agent_SetFreeBufferObjHold(pAgent, value);
            }
        }

        /// <summary>
        /// 读取或设置心跳包间隔（毫秒，0 则不发送心跳包））
        /// </summary>
        public uint KeepAliveTime
        {
            get
            {
                return Sdk.HP_TcpAgent_GetKeepAliveTime(pAgent);
            }
            set
            {
                Sdk.HP_TcpAgent_SetKeepAliveTime(pAgent, value);
            }
        }

        /// <summary>
        /// 读取或设置心跳确认包检测间隔（毫秒，0 不发送心跳包，如果超过若干次 [默认：WinXP 5 次, Win7 10 次] 检测不到心跳确认包则认为已断线）
        /// </summary>
        public uint KeepAliveInterval
        {
            get
            {
                return Sdk.HP_TcpAgent_GetKeepAliveInterval(pAgent);
            }
            set
            {
                Sdk.HP_TcpAgent_SetKeepAliveInterval(pAgent, value);
            }
        }

        /// <summary>
        /// 读取或设置是否标记静默时间（设置为 TRUE 时 DisconnectSilenceConnections() 和 GetSilencePeriod() 才有效，默认：FALSE）
        /// </summary>
        public bool IsMarkSilence
        {
            get
            {
                return Sdk.HP_Agent_IsMarkSilence(pAgent);
            }
            set
            {
                Sdk.HP_Agent_SetMarkSilence(pAgent, value);
            }
        }

        /// <summary>
        /// 读取或设置数据发送策略
        /// </summary>
        public SendPolicy SendPolicy
        {
            get
            {
                return Sdk.HP_Agent_GetSendPolicy(pAgent);
            }
            set
            {
                Sdk.HP_Agent_SetSendPolicy(pAgent, value);
            }
        }

        /// <summary>
        /// 获取或设置 OnSend 事件同步策略
        /// </summary>
        public OnSendSyncPolicy OnSendSyncPolicy
        {
            get
            {
                return Sdk.HP_Agent_GetOnSendSyncPolicy(pAgent);
            }
            set
            {
                Sdk.HP_Agent_SetOnSendSyncPolicy(pAgent, value);
            }
        }

        /// <summary>
        /// 根据错误码返回错误信息
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public string GetSocketErrorDesc(SocketError code)
        {
            IntPtr ptr = Sdk.HP_GetSocketErrorDesc(code);
            string desc = Marshal.PtrToStringAnsi(ptr);
            return desc;
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        protected Sdk.OnPrepareConnect _OnPrepareConnect = null;
        protected Sdk.OnConnect _OnConnect = null;
        protected Sdk.OnReceive _OnReceive = null;
        protected Sdk.OnSend _OnSend = null;
        protected Sdk.OnClose _OnClose = null;
        protected Sdk.OnShutdown _OnShutdown = null;
        protected Sdk.OnHandShake _OnHandShake = null;

        /// <summary>
        /// 设置回调函数
        /// </summary>
        protected virtual void SetCallback()
        {
            _OnPrepareConnect = new Sdk.OnPrepareConnect(SDK_OnPrepareConnect);
            _OnConnect = new Sdk.OnConnect(SDK_OnConnect);
            _OnSend = new Sdk.OnSend(SDK_OnSend);
            _OnReceive = new Sdk.OnReceive(SDK_OnReceive);
            _OnClose = new Sdk.OnClose(SDK_OnClose);
            _OnShutdown = new Sdk.OnShutdown(SDK_OnShutdown);
            _OnHandShake = new Sdk.OnHandShake(SDK_OnHandShake);

            // 设置 Socket 监听器回调函数
            Sdk.HP_Set_FN_Agent_OnPrepareConnect(pListener, _OnPrepareConnect);
            Sdk.HP_Set_FN_Agent_OnConnect(pListener, _OnConnect);
            Sdk.HP_Set_FN_Server_OnSend(pListener, _OnSend);
            Sdk.HP_Set_FN_Server_OnReceive(pListener, _OnReceive);
            Sdk.HP_Set_FN_Server_OnClose(pListener, _OnClose);
            Sdk.HP_Set_FN_Agent_OnShutdown(pListener, _OnShutdown);
            Sdk.HP_Set_FN_Server_OnHandShake(pListener, _OnHandShake);

        }

        protected HandleResult SDK_OnHandShake(IntPtr pSender, IntPtr connId)
        {
            if (OnHandShake != null)
            {
                return OnHandShake(connId);
            }
            return HandleResult.Ignore;
        }

        protected virtual HandleResult SDK_OnPrepareConnect(IntPtr pSender, IntPtr connId, IntPtr socket)
        {
            if (OnPrepareConnect != null)
            {
                return OnPrepareConnect(connId, socket);
            }
            return HandleResult.Ignore;
        }

        protected virtual HandleResult SDK_OnConnect(IntPtr pSender, IntPtr connId)
        {
            if (OnConnect != null)
            {
                return OnConnect(connId);
            }
            return HandleResult.Ignore;
        }

        protected virtual HandleResult SDK_OnSend(IntPtr pSender, IntPtr connId, IntPtr pData, int length)
        {
            if (OnSend != null)
            {
                byte[] bytes = new byte[length];
                Marshal.Copy(pData, bytes, 0, length);
                return OnSend(connId, bytes);
            }
            return HandleResult.Ignore;
        }

        protected virtual HandleResult SDK_OnReceive(IntPtr pSender, IntPtr connId, IntPtr pData, int length)
        {
            if (OnPointerDataReceive != null)
            {
                return OnPointerDataReceive(connId, pData, length);
            }
            else if (OnReceive != null)
            {
                byte[] bytes = new byte[length];
                Marshal.Copy(pData, bytes, 0, length);
                return OnReceive(connId, bytes);
            }
            return HandleResult.Ignore;
        }

        protected virtual HandleResult SDK_OnClose(IntPtr pSender, IntPtr connId, SocketOperation enOperation, int errorCode)
        {
            if (OnClose != null)
            {
                return OnClose(connId, enOperation, errorCode);
            }
            return HandleResult.Ignore;
        }

        protected virtual HandleResult SDK_OnShutdown(IntPtr pSender)
        {
            if (OnShutdown != null)
            {
                return OnShutdown();
            }
            return HandleResult.Ignore;
        }


        /////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 获取系统返回的错误码
        /// </summary>
        /// <returns></returns>
        public int SYSGetLastError()
        {
            return Sdk.SYS_GetLastError();
        }

        /// <summary>
        /// 调用系统的 ::WSAGetLastError() 方法获取通信错误代码
        /// </summary>
        /// <returns></returns>
        public int SYSWSAGetLastError()
        {
            return Sdk.SYS_WSAGetLastError();
        }

        /// <summary>
        /// 调用系统的 setsockopt()
        /// </summary>
        /// <param name="sock"></param>
        /// <param name="level"></param>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        /// 
        public int SYS_SetSocketOption(IntPtr sock, int level, int name, IntPtr val, int len)
        {
            return Sdk.SYS_SetSocketOption(sock, level, name, val, len);
        }

        /// <summary>
        /// 调用系统的 getsockopt()
        /// </summary>
        /// <param name="sock"></param>
        /// <param name="level"></param>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        /// 
        public int SYSGetSocketOption(IntPtr sock, int level, int name, IntPtr val, ref int len)
        {
            return Sdk.SYS_GetSocketOption(sock, level, name, val, ref len);
        }
        /// <summary>
        /// 调用系统的 ioctlsocket()
        /// </summary>
        /// <param name="sock"></param>
        /// <param name="cmd"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        /// 
        public int SYSIoctlSocket(IntPtr sock, long cmd, IntPtr arg)
        {
            return Sdk.SYS_IoctlSocket(sock, cmd, arg);
        }

        /// <summary>
        /// 调用系统的 ::WSAIoctl()
        /// </summary>
        /// <param name="sock"></param>
        /// <param name="dwIoControlCode"></param>
        /// <param name="lpvInBuffer"></param>
        /// <param name="cbInBuffer"></param>
        /// <param name="lpvOutBuffer"></param>
        /// <param name="cbOutBuffer"></param>
        /// <param name="lpcbBytesReturned"></param>
        /// <returns></returns>
        public int SYS_WSAIoctl(IntPtr sock, uint dwIoControlCode, IntPtr lpvInBuffer, uint cbInBuffer,
                                              IntPtr lpvOutBuffer, uint cbOutBuffer, uint lpcbBytesReturned)
        {
            return Sdk.SYS_WSAIoctl(sock, dwIoControlCode, lpvInBuffer, cbInBuffer,
                                            lpvOutBuffer, cbOutBuffer, lpcbBytesReturned);
        }

        /// <summary>
        /// 由结构体转换为byte数组
        /// </summary>
        public byte[] StructureToByte<T>(T structure)
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] buffer = new byte[size];
            IntPtr bufferIntPtr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structure, bufferIntPtr, true);
                Marshal.Copy(bufferIntPtr, buffer, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(bufferIntPtr);
            }
            return buffer;
        }

        /// <summary>
        /// 由byte数组转换为结构体
        /// </summary>
        public T ByteToStructure<T>(byte[] dataBuffer)
        {
            object structure = null;
            int size = Marshal.SizeOf(typeof(T));
            IntPtr allocIntPtr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(dataBuffer, 0, allocIntPtr, size);
                structure = Marshal.PtrToStructure(allocIntPtr, typeof(T));
            }
            finally
            {
                Marshal.FreeHGlobal(allocIntPtr);
            }
            return (T)structure;
        }

        /// <summary>
        /// 对象序列化成byte[]
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public byte[] ObjectToBytes(object obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                return ms.GetBuffer();
            }
        }

        /// <summary>
        /// byte[]序列化成对象
        /// </summary>
        /// <param name="Bytes"></param>
        /// <returns></returns>
        public object BytesToObject(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                IFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(ms);
            }
        }

        /// <summary>
        /// byte[]转结构体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public T BytesToStruct<T>(byte[] bytes)
        {
            Type strcutType = typeof(T);
            int size = Marshal.SizeOf(strcutType);
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, buffer, size);
                return (T)Marshal.PtrToStructure(buffer, strcutType);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
