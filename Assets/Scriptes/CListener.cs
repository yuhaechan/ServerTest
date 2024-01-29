using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Net;


class CListener
{
    // 비동기 Accept를 위한 EventArgs
    SocketAsyncEventArgs accept_args;

    // 클라이언트의 접속을 처리할 소켓
    Socket listen_socket;

    // Accept 처리의 순서를 제어하기 위한 이벤트 변수
    AutoResetEvent flow_control_event;

    // 새로운 클라이언트가 접속했을 때 호출되는 델리게이트
    public delegate void NewclientHandler(Socket client_socket, object token);
    public NewclientHandler callback_on_newclient;

    public CListener()
    {
        this.callback_on_newclient = null;
    }

    public void start(string host, int port, int backlog)
    {
        // 소켓을 생성한다.
        this.listen_socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);

        IPAddress address;
        if (host == "0.0.0.0")
        {
            address = IPAddress.Any;
        }
        else
        {
            address = IPAddress.Parse(host);
        }
        IPEndPoint endpoint = new IPEndPoint(address, port);

        try
        {
            //소켓에 host 정보를 바인딩시킨 뒤 Listen 메서드를 호출하여 대기한다.
            this.listen_socket.Bind(endpoint);
            this.listen_socket.Listen(backlog);
            
            // Completed 프로퍼티에 이벤트 핸들러 객체를 연결해 주고 AcceptAsync 를 호출할 때 파라미터로 넘겨주기만 하면 된다.
            //닷넷 비동기 소켓은 메서드 호출 -> 완료 통지 순서로 이루어짐.
            this.accept_args = new SocketAsyncEventArgs();
            this.accept_args.Completed += new EventHandler<SocketAsyncEventArgs>(on_accept_completed);

            // 클라이언트가 들어오기를 기다림.
            // 비동기 메서드이므로 블로킹되지 않고 바로 리턴됨
            // 콜백 메서드를 통해서 접속 통보를 받는다.

            this.listen_socket.AcceptAsync(this.accept_args);

#if UNITY_EDITOR
            // this.listen_socket.AcceptAsync(this.accept_args); 대체 ios에서는 accept처리 안되는 버그가 발생할 수도 있음
            Thread listen_thread = new Thread(do_listen);
            listen_thread.Start();
#endif
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    void do_listen()
    {
        // accept 처리 제어를 위해 이벤트 객체를 생성
        this.flow_control_event = new AutoResetEvent(false);

        while (true)
        {
            // SocketAsync EventArgs 를 재사용하기 위해서 null로 만듬
            this.accept_args.AcceptSocket = null;

            bool pending = true;

            try
            {
                // 비동기 accept 를 호출하여 클라이언트의 접속을 받아들임
                // 비동기 메서드이지만 동기적으로 수행이 완료될 경우도 있으니
                // 리턴 값을 확인하여 분기 처리를 해주어야함.
                pending = listen_socket.AcceptAsync(this.accept_args);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                continue;
            }

            // 즉시 완료 (리턴 값이 false 일 때) 시
            // 이벤트가 발생하지 않아 콜백 메서드를 직접 호출해야줘야함.
            // pending 상태라면 비동기 요청이 들어간 상태라 콜백 메서드를 기다리면 된다.
            if (!pending)
            {
                on_accept_completed(null, this.accept_args);
            }

            // 클라이언트 접속 처리가 완료되면 이벤트 객체의 신호를 전달받아 다시 루프를 수행.
            this.flow_control_event.WaitOne();
        }
    }
    //this.flow_control_event = new AutoResetEvent(false);

    void on_accept_completed(object sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            // 새로 생긴 소켓을 보관해 놓은 뒤
            Socket client_socket = e.AcceptSocket;

            // 다음 연결을 받아들인다.
            this.flow_control_event.Set();

            // 이 클래스에서는 accept까지의 역할만 수행하고 클라이언트의 접속 이후의 처리는
            // 외부로 넘기기 위해서 콜백 메서드를 호출해 주도록 한다.
            // 그 이유는 소켓 처리부와 콘텐츠 구현부를 분리하기 위해서다.
            // 상대적으로 변경이 적은 부분이기 떄문에 양쪽을 분리시켜 주는 것이 좋다.
            // 또한, 클래스 설계 방침에 따라 Listen 에 관련된 코드만 존재하도록  하기 위함.
            if (this.callback_on_newclient != null)
            {
                this.callback_on_newclient(client_socket, e.UserToken); // 이게 CListener 클래스의 마지막 역활
            }
            return;
        }
        else
        {
            // Accept 실패 처리.
            Debug.Log("Failed to accept client");
        }
        // 다음 연결을 받아들인다.
        this.flow_control_event.Set();
    }


}
