using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Unity.VisualScripting;

// 클라이언트의 접속을 받아들이기 위한 Listener 객체 설정
// 클라이언트의 접속이 이루어질 때 통보할 이벤트 처리
// 서버에 접속하는 모든 클라이언트에 대한 송/수신 데이터 버퍼 관리
class CNetworkService
{
    // 클라이언트의 접속을 받아들이기 위한 객체
    CListener client_listener;

    // 메시지 수신, 전송 시 필요한 객체
    //기존에는 Begin ~ End 계열의 API를 사용 (?)
    SocketAsyncEventArgsPool receive_event_args_pool;
    SocketAsyncEventArgsPool send_event_args_pool;

    // 메시지 수신, 전송 시 닷넷 비동기 소켓에서 사용할 버퍼를 관리하는 객체
    // Buffer란?
    // 버퍼는 네트워크 통신이 지속되는 동안에 계속해서 사용하는 메모리이기 떄문에
    // 풀링하여 메모리를 재사용
    BufferManager buffer_manager;

    // 클라이언트의 접속이 이루어졌을 떄 호출되는 델리게이트
    public delegate void SessionHandler(CUserToken token);
    public SessionHandler session_created_callback {get; set;}

    public void listen (string host, int port, int backlog)
    {
        CListener listener = new CListener();
        listener.callback_on_newclient += on_new_client;
        listener.start(host, port, backlog);
    }


    void on_new_client (Socket client_socket, object token)
    {
        // 풀에서 하나 꺼내와 사용한다.
        SocketAsyncEventArgs receive_args = this.receive_event_args_pool.Pop();
        SocketAsyncEventArgs send_args = this.send_event_args_pool.Pop();

        //SocketAsyncEventArgs를 생성할 때 만들어 두었던 CUserToken을 꺼내와서
        // 콜백 매서드의 파라미터로 넘겨준다.
        if (this.session_created_callback != null)
        {
            CUserToken user_token = receive_args.UserToken as CUserToken;
            this.session_created_callback(user_token);
        }

        // 클라이언트로부터 데이터를 수신할 준비를 한다.
        begin_receive(client_socket, receive_args, send_args);
    }

    void begin_receive (Socket socket, SocketAsyncEventArgs receive_args, SocketAsyncEventArgs send_args)
    {
        //receive_args, send_args 아무곳에서나 꺼내와도 된다. 둘다 동일한 CUserToken을 사용하기때문
        CUserToken token = receive_args.UserToken as CUserToken;
        token.set_event_args(receive_args, send_args);

        // 생성된 클라이언트 소켓을 보관해 놓고 통신할 때 사용한다.
        token.socket = socket;

        // 데이터를 받을 수 있도록 수신 메서드를 호출해준다.
        // 비동기로 수신될 경우 워커 스레드에서 대기 중으로 있다가 Completed에 설정해놓은 메서드가 호출된다.
        // 동기로 완료될 경우에는 직접 완료 메서드를 호출해줘야 한다.
        bool pending = socket.ReceiveAsync(receive_args);
        if (!pending)
        {
            process_receive(receive_args);
        }
    }

    void receive_completed(object sender, SocketAsyncEventArgs e)
    {
        if (e.LastOperation == SocketAsyncOperation.Receive)
        {
            process_receive(e);
            return;
        }

        throw new ArgumentException ( "The last operation completed on the socket was not a receive. " );
    }

    private void process_receive(SocketAsyncEventArgs e)
    {
        // check if the remote host closed the connection
        CUserToken token = e.UserToken as CUserToken;
        if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
        {
            //이후 작업은 CUserToken에서 수행
            token.on_receive(e.Buffer, e.Offset, e.BytesTransferred);

            // 다음 메시지 수신을 위해서 다시 ReceiveAsync 메서드를 호출
            bool pending = token.socket.ReceiveAsync(e);
            if (!pending)
            {
                process_receive(e);
            }
        }
        else
        {
            Console.WriteLine(string.Format("error {0}, transferred {1}, e.SocketError, e.BytesTransferred"));
            close_Clientsocket(token);
        }

        token.on_receive(e.Buffer, e.Offset, e.BytesTransferred);
    }

    private void close_Clientsocket(CUserToken token)
    {
        throw new NotImplementedException();
    }

    public CNetworkService(int max_connections)
    {
        /*
        // socketAsyncEventArgs 객체를 미리 생성하여 풀에 너어두는 코드
        
        this.receive_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);
        this.send_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);

        for(int i=0; i<max_connections; i++)
        {
            CUserToken token = new CUserToken();
            SocketAsyncEventArgs arg;
            //receive pool;
            {
                //Pre-allocate a set of reusable SocketAsyncEventArgs
                arg = new SocketAsyncEventArgs();
                arg.Completed += new EventHandler<SocketAsyncEventArgs>(receive_Completed);
                arg.UserToken = token;

                // add SocketAsyncEventArg to the pool
                this.receive_event_args_pool.Push(args);
            }

            // send pool
            {
                // Pre-allocate a set of reusable SocketAsyncEventArgs
                arg = new SocketAsyncEventArgsPool();
                arg.Completed += new EventHandler<SocketAsyncEventArgsPool>(send_completed);
                arg.UserToken = token;

                // add SocketAsyncEventArg to the pool
                this.send_event_args_pool.Push(args);
            }
            
            BufferManager buffer_manager;
            this.buffer_manager = new BufferManager(this.max_connections * this.buffer_size * this.pre_alloc_count, this.buffer_size);
            
        }
        */
    }

}