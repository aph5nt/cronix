//namespace Cronix.Tests
//
//open Xunit
//open FsUnit.Xunit
//open Logging
//open System
//open Owin
//open Microsoft.AspNet.SignalR.Hubs
//open Microsoft.AspNet.SignalR
//
//
//module HubTests =
//    open Cronix.Hubs
//    open Cronix
//    open Foq
//
//    let scheduleManagerMock = 
//            let event = new Event<TriggerDetail>()
//            let mock = new Mock<IScheduleManager>()
//            mock.Setup(fun x -> <@ x.OnTriggerStateChanged@>).Returns(event.Publish).Create()
//    let called = ref false
//    let clientsMock =
//        let clients = new Mock<IHubCallerConnectionContext<IScheduleManagerHub>>()
//        clients.Setup(fun x -> <@ x.All.GetData([||]) @>).Calls<TriggerDetail[]>(fun x -> called := true).Create()    
//
//    //[<Fact>]
//    let ``Getdata``() =
//
//        // Arrange
//        GlobalHost.DependencyResolver.Register(typeof<IScheduleManager>, fun() -> scheduleManagerMock :> obj)
//        let hub = new ScheduleManagerHub()
//        hub.Clients <- clientsMock
//
//        // Act
//        hub.GetData()
//
//        // Assert
//        called |> should equal true