
using System;
using System.Diagnostics;
using System.Net.Sockets;
using Unity.VisualScripting.Antlr3.Runtime;

class CUserToken
{
    public object? UserToken {get; set;}
    public Socket socket;

    public void on_receive(byte[] buffer, int offset,  int transfered)
    {
        this.message_resolver.on_receive(buffer, offset, transfered, on_message);
    }

    public void set_event_args(SocketAsyncEventArgs recieve, SocketAsyncEventArgs send)
    {

    }

    /// <summary>
    /// 패킷을 전송한다.
    /// 큐가 비어있을 경우에는 큐에 추가한 뒤 바로 SendAsync 메서드를 호출하고,
    /// 데이터가 들어이을 경우에는 새로 추가한다.
    /// 
    /// 큐잉된 패킷의 전송 시점 : 현재 진행 중인 SendAsync가 완료되었을 때 큐를 검사하여 나머지 패킷을 전송한다.
    /// </summary>
    /// <param name="msg"></param>
    public void send (CPacket msg)
    {
        CPacket clone = newCPacket();
        msg.copy_to(clone);

        lock (this.cs_sending_queue)
        {
            // 큐가 비어 있다면 큐에 추가하고 바로 비동기 전송 메서드를 호출한다.
            if (this.sending_queue.Count <= 0)
            {
                this.sending_queue.Enqueue(msg);
                start_sen();
                return;
            }

            // 큐에 무언가가 들어 있다면 아직 이전 전송이 완료되지 않은 상태이므로
            // 큐에 추가만 하고 리턴한다.
            // 현재 수행 중인 SendAsync가 완료된 이후에 큐를 검사하여 데이터가 있으면
            // SendAsync를 호출하여 전송해줄 것이다.
            this.sending_queue.Enqueue(msg);
        }
    }

    /// <summary>
    /// 비동기 전송을 시작한다.
    /// </summary>
    void start_send()
    {
        lock (this.cs_sending_queue)
        {
            // 전송이 아직 완료된 상태가 아니므로 데이터만 가져오고 큐에서 제거하진 않는다.
            CPacket msg = this.sendsending_queue.Peek();

            // 헤더에 패킷 사이즈를 기록한다.
            msg.record_size();

            // 이번에 보낼 패킷 사이즈만큼 버퍼 크기를 설정하고
            this.send_event_args.SetBuffer(this.send_event_args.Offset, msg.position);

            // 패킷 내용을 SocketAsyncEventArgs 버퍼에 복사한다.
            Array.Copy(msg.buffer, 0, this.send_event_args.Buffer, this.send_event_args.Offset, msg.position);

            // 비동기 전송 시작.
            bool pending = this.socket.SendAsync(this.send_event_args);
            if (!pending)
            {
                process_send(this.send_event_args);
            }
        }
    }

    public void process_send(SocketAsyncEventArgs e)
    {
        
    }

}
