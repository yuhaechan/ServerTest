using System;
using System.Collections.Generic;
using System.Net.Sockets;

class SocketAsyncEventArgsPool
{
    Stack<SocketAsyncEventArgs> m_pool;

    // Initializes the object pool to the specified size
    //
    // The "capacity" parameter is the maximum number of
    // SocketAsync<EventArgs objects the pool can hold

    public SocketAsyncEventArgsPool(int capacity)
    {
        m_pool = new Stack<SocketAsyncEventArgs>(capacity);

        for (int i = 0; i < capacity; i++)
        {
            SocketAsyncEventArgs arg = new SocketAsyncEventArgs();
            m_pool.Push(arg);
        }
    }

    // Add a SocketAsyncEventArg instance to the pool
    //
    // The "item" parameter is the SocketAsyncEventArgs instance
    // to add to the pool

    public void Push(SocketAsyncEventArgs item)
    {
        if (item == null) 
        {
            throw new ArgumentNullException ("Items added to aa SocketAsyncEventArgsPool cannot be null");
        }
        lock (m_pool)
        {
            m_pool.Push(item);
        }
        //lock 키워드를 사용하면 특정 코드 블록이 실행될 때에만 하나의 스레드만 접근할 수 있도록 보장됩니다. 
        //이를 통해 여러 스레드에서 동시에 공유 데이터에 접근할 때 발생할 수 있는 경쟁 상태나 데드락과 같은 문제를 방지할 수 있습니다.
        //즉, lock 키워드는 공유 자원에 대한 동기화를 제공하여 여러 스레드 간의 안전한 데이터 접근을 보장합니다.
    } 

    // Removes a SocketAsyncEventArgs instance from the pool
    // and returns the object removed from the pool
    public SocketAsyncEventArgs Pop()
    {
        lock (m_pool)
        {
            return m_pool.Pop();
        }
    }

    // The number of SocketAsyncEventArgs instances in the pool
    public int Count
    {
        get { return m_pool.Count; }
    }

}