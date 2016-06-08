Imports mujinplccs

Module Program

    Sub Main()
        Dim memory = New PLCMemory()
        Dim logic = New PLCLogic(New PLCController(memory, TimeSpan.FromSeconds(1)))
        Dim server = New PLCServer(memory, "tcp://*:5555")

        Console.WriteLine("Starting server to listen on {0} ...", server.Address)
        server.Start()

        Console.WriteLine("Waiting for controller connection ...")
        logic.WaitUntilConnected()
        Console.WriteLine("Controller connected.")

        Try
            Console.WriteLine("Starting order cycle ...")
            Dim status = logic.StartOrderCycle("123", "coffeebox", 10)
            Console.WriteLine("Order cycle started. numLeftInOrder = {0}, mumLeftInSupply = {1}.", status.NumLeftInOrder, status.NumLeftInSupply)

            While True
                status = logic.WaitForOrderCycleStatusChange()
                If Not status.IsRunningOrderCycle Then
                    Console.WriteLine("Cycle finished. {0}", status.OrderCycleFinishCode)
                    Exit While
                End If
                Console.WriteLine("Cycle running. numLeftInOrder = {0}, mumLeftInSupply = {1}.", status.NumLeftInOrder, status.NumLeftInSupply)
            End While

        Catch e As PLCLogic.PLCError
            Console.WriteLine("PLC Error. {0}. {1}x{2}", e.Message, CInt(e.ErrorCode), e.DetailedErrorCode)
        End Try

        Console.WriteLine("Press any key to exit.")
        Console.ReadKey(True)

        server.Stop()
    End Sub

End Module
