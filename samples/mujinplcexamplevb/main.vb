Imports mujinplccs
Imports System.Threading
Imports System

Module main

    Sub Main()
        Dim server As PLCServer
        server = New PLCServer("tcp://*:5555")
        server.Start()
        Console.WriteLine("Server started and listening on {0} ...", server.Address)
        Console.WriteLine("Press any key to exit.")
        Console.ReadKey(True)
        server.Stop()
    End Sub

End Module
