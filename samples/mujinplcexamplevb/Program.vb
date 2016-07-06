Imports mujinplccs

Module Program

    Sub Main()
        Dim memory = New PLCMemory()
        Dim logic = New PLCLogic(New PLCController(memory, TimeSpan.FromSeconds(1.0)))
        Dim server = New PLCServer(memory, "tcp://*:5555")

        REM Console.WriteLine("Starting server to listen on {0} ...", server.Address)
        server.Start()

        REM Console.WriteLine("Waiting for controller connection ...")
        logic.WaitUntilConnected()
        REM Console.WriteLine("Controller connected.")

        Try
            REM Console.WriteLine("Starting order cycle ...")
            Dim status = logic.StartOrderCycle("123", "coffeebox", 10)
            REM Console.WriteLine("Order cycle started. numLeftInOrder = {0}, numLeftInLocation1 = {1}.", status.numLeftInOrder, status.numLeftInLocation1)

            While True
                status = logic.WaitForOrderCycleStatusChange()
                If Not status.isRunningOrderCycle Then
                    REM Console.WriteLine("Cycle finished. {0}", status.orderCycleFinishCode)
                    Exit While
                End If
                REM Console.WriteLine("Cycle running. numLeftInOrder = {0}, numLeftInLocation1 = {1}.", status.numLeftInOrder, status.numLeftInLocation1)
            End While

        Catch e As PLCLogic.PLCError
            REM Console.WriteLine("PLC Error. {0}. {1}x{2}", e.Message, CInt(e.ErrorCode), e.DetailedErrorCode)
        End Try

        REM Console.WriteLine("Press any key to exit.")
        REM Console.ReadKey(True)

        server.Stop()
    End Sub

End Module
