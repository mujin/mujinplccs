Imports mujinplccs

Module Program

    Sub Main()
        Dim memory = New PLCMemory()
        Dim controller = New PLCController(memory, TimeSpan.FromSeconds(1.0))
        Dim logic = New PLCLogic(controller)
        Dim server = New PLCServer(memory, "tcp://*:5555")

        REM Console.WriteLine("Starting server to listen on {0} ...", server.Address)
        server.Start()

        REM Console.WriteLine("Waiting for controller connection ...")
        logic.WaitUntilConnected()
        REM Console.WriteLine("Controller connected.")
        
        Try
            REM Console.WriteLine("Starting order cycle ...");
            If controller.GetBoolean("isError") Then
                REM Console.WriteLine("controller is in error 0x{0:X}, resetting", controller.Get("errorcode"));
                logic.ResetError();
            End If

            If controller.GetBoolean("isRunningOrderCycle") Then
                REM Console.WriteLine("previous cycle already running, so stop and wait");
                logic.StopOrderCycle();
            End If

            REM Console.WriteLine("Waiting for cycle ready...");
            logic.WaitUntilOrderCycleReady();


            REM Console.WriteLine("Starting order cycle ...")
            REM For work2 use: controller.Set("robotId",2); StartOrderCycle("123", "work2_b", 1)
            controller.Set("robotId",1);
            Dim status = logic.StartOrderCycle("123", "work1", 1)
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
            REM Console.WriteLine("PLC Error. {0}.", e.Message)
        End Try

        REM Console.WriteLine("Press any key to exit.")
        REM Console.ReadKey(True)

        server.Stop()
    End Sub

End Module
