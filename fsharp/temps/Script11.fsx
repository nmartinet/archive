#I @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5"
#r @"WindowsBase.dll"
#r @"PresentationCore.dll"
#r @"PresentationFramework.dll"
#r @"System.Xaml.dll"
#r @"UIAutomationProvider.dll"
#r @"UIAutomationTypes.dll"


module WPFEventLoop =     
    open System    
    open System.Windows    
    open System.Windows.Threading    
    open Microsoft.FSharp.Compiler.Interactive    
    open Microsoft.FSharp.Compiler.Interactive.Settings    

    type RunDelegate<'b> = delegate of unit -> 'b     
    let Create() =         
        let app  =             
            try                 
                // Ensure the current application exists. This may fail, if it already does.                
                let app = new Application() in                 
                // Create a dummy window to act as the main window for the application.                
                // Because we're in FSI we never want to clean this up.                
                new Window() |> ignore;                 
                app              
            with :? InvalidOperationException -> Application.Current        
        let disp = app.Dispatcher        
        let restart = ref false        
        { new IEventLoop with             
            member x.Run() =                    
                app.Run() |> ignore                 
                !restart             

            member x.Invoke(f) =                  
                try 
                    disp.Invoke(DispatcherPriority.Send,new RunDelegate<_>(fun () -> box(f ()))) |> unbox                 
                with e -> eprintf "\n\n ERROR: %O\n" e; reraise()             

            member x.ScheduleRestart() =   ()                 
            //restart := true;                 
            //app.Shutdown()        
         }     

    let Install() = fsi.EventLoop <-  Create()

WPFEventLoop.Install()

// ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ //

open System
open System.Windows
open System.Windows.Data 
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Media  
open System.ComponentModel 

// ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ //

type ucData() as this =
    inherit Windows.Controls.UserControl()

    // ---------------------------------------------------------------------------------------------------------------- //

    static let dpDescription = 
        DependencyProperty.Register("Description", typeof<string>, typeof<ucData>)

    static let dpData = 
        DependencyProperty.Register("Data", typeof<double>, typeof<ucData>)

    static let dpUnitOfMeasure = 
        DependencyProperty.Register("UnitOfMeasure", typeof<string>, typeof<ucData>) 

    // ---------------------------------------------------------------------------------------------------------------- //

    let descriptionChangedEvent = new  Event<RoutedEventHandler, RoutedEventArgs>()

    let dataChangedEvent = new Event<RoutedEventHandler, RoutedEventArgs>()

    let unitOfMeasureChangedEvent = new Event<RoutedEventHandler, RoutedEventArgs>()

    // ---------------------------------------------------------------------------------------------------------------- //

    let grid = 
        let c = new Grid()
        let colDesc = new ColumnDefinition()
        colDesc.MinWidth <- 100.
        colDesc.Width <- System.Windows.GridLength(100.,GridUnitType.Star)
        c.ColumnDefinitions.Add( colDesc )
        let colData = new ColumnDefinition()
        colData.MinWidth <- 70.
        colDesc.Width <- System.Windows.GridLength(70.,GridUnitType.Star)
        c.ColumnDefinitions.Add( colData )
        let colUnit = new ColumnDefinition()
        colUnit.MinWidth <- 70.
        colUnit.Width <- System.Windows.GridLength(70.,GridUnitType.Star)
        c.ColumnDefinitions.Add( colUnit )
        c

    // ---------------------------------------------------------------------------------------------------------------- //

    let desc = 
        let c = new Label()
        c.Margin<- new Thickness(3.0)
        c.SetValue(Grid.ColumnProperty,0)
        c

    // ---------------------------------------------------------------------------------------------------------------- //

    let data= 
        let c = new TextBox()
        c.Margin<- new Thickness(3.0)
        c.SetValue(Grid.ColumnProperty,1)
        c

    // ---------------------------------------------------------------------------------------------------------------- //

    let unit = 
        let c = new Label()
        c.Margin<- new Thickness(3.0)
        c.SetValue(Grid.ColumnProperty,2)
        c

    // ---------------------------------------------------------------------------------------------------------------- //

    let condassign a b c = 
        match a with
        |   true -> b
        | false -> c

    // ---------------------------------------------------------------------------------------------------------------- //

    do        
        grid.Children.Add(desc) |> ignore
        grid.Children.Add(data) |> ignore
        grid.Children.Add(unit) |> ignore
        //this.Content <- grid |> ignore
        this.AddChild(grid) |> ignore

        // ---------------------------------------------------------------------------------------------------------------- //

        this.Data <- 0.00
        this.Description <- "<Description>"
        this.UnitOfMeasure <- "<UM>"

        // ---------------------------------------------------------------------------------------------------------------- //

        this.DataContext <- this 

        // ---------------------------------------------------------------------------------------------------------------- //

        let bDesc = new Binding()
        bDesc.Path <- new PropertyPath("Description")
        bDesc.Mode <- BindingMode.OneWay
        bDesc.RelativeSource <- new RelativeSource(RelativeSourceMode.FindAncestor,typeof<ucData>,1)

        let bData = new Binding()
        bData.Path <- new PropertyPath("Data")
        bData.Mode <- BindingMode.TwoWay 
        bData.RelativeSource <- new RelativeSource(RelativeSourceMode.FindAncestor,typeof<ucData>,1)

        let bUnit = new Binding()
        bUnit.Path <- new PropertyPath("UnitOfMeasure")
        bUnit.Mode <- BindingMode.OneWay
        //bUnit.RelativeSource <- new RelativeSource(RelativeSourceMode.FindAncestor,typeof<ucData>,1) 
        bUnit.Source <- this

        // ---------------------------------------------------------------------------------------------------------------- //

        desc.SetBinding(Label.ContentProperty, bDesc) |> ignore
        data.SetBinding(TextBox.TextProperty, bData) |> ignore
        unit.SetBinding(Label.ContentProperty, bUnit) |> ignore

    // ---------------------------------------------------------------------------------------------------------------- //

    static member DescriptionProperty = dpDescription

    [<Description("Description of the Numerical Data"); Category("UserData")>]
    member x.Description
        with get() =
            let res = x.GetValue(ucData.DescriptionProperty) 
            (res :?> string)
        and set (v:string) = 
            x.SetValue(ucData.DescriptionProperty, (condassign (v=null) " " v) )

    // ---------------------------------------------------------------------------------------------------------------- //

    [<CLIEvent>]
    member x.DescriptionChanged = descriptionChangedEvent.Publish 

    static member DescriptionChangedEvent =
        EventManager.RegisterRoutedEvent
            ("DescriptionChanged", RoutingStrategy.Bubble, 
                typeof<RoutedEventHandler>, typeof<ucData>)

    member x.OnDescriptionChangedEvent() =
        let argsEvent = new RoutedEventArgs()
        argsEvent.RoutedEvent <- ucData.DescriptionChangedEvent 
        argsEvent.Source <- x
        descriptionChangedEvent.Trigger(this, argsEvent)

    // ---------------------------------------------------------------------------------------------------------------- //

    static member DataProperty =dpData

    [<Description("Specify the Numerical Data"); Category("UserData")>]
    member x.Data
        with get() = 
            let res = x.GetValue(ucData.DataProperty) 
            (res :?> double)
        and set (v:double) = 
            x.SetValue(ucData.DataProperty, v )

    // ---------------------------------------------------------------------------------------------------------------- //

    [<CLIEvent>]
    member x.DataChanged = dataChangedEvent.Publish 

    static member DataChangedEvent =
        EventManager.RegisterRoutedEvent
            ("DataChanged", RoutingStrategy.Bubble, 
                typeof<RoutedEventHandler>, typeof<ucData>)

    member x.OnDataChangedEvent() =
        let argsEvent = new RoutedEventArgs()
        argsEvent.RoutedEvent <- ucData.DataChangedEvent 
        argsEvent.Source <- x
        dataChangedEvent.Trigger(this, argsEvent)

    // ---------------------------------------------------------------------------------------------------------------- //

    static member UnitOfMeasureProperty = dpUnitOfMeasure

    [<Description("Specify the 'Unit of Measure'"); Category("UserData")>]
    member x.UnitOfMeasure
        with get() = 
            let res = x.GetValue(ucData.UnitOfMeasureProperty) 
            (res :?> string)
        and set (v:string) = 
            x.SetValue(ucData.UnitOfMeasureProperty, (condassign (v=null) " " v) )

    // ---------------------------------------------------------------------------------------------------------------- //

    [<CLIEvent>]
    member x.UnitOfMeasureChanged = unitOfMeasureChangedEvent.Publish 

    static member UnitOfMeasureChangedEvent=
        EventManager.RegisterRoutedEvent
            ("UnitOfMeasureChanged", RoutingStrategy.Bubble, 
                typeof<RoutedEventHandler>, typeof<ucData>)

    member x.OnUnitOfMeasureChangedEvent() =
        let argsEvent = new RoutedEventArgs()
        argsEvent.RoutedEvent <- ucData.UnitOfMeasureChangedEvent
        argsEvent.Source <- x
        unitOfMeasureChangedEvent.Trigger(this, argsEvent)

// ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ //

type TestWindow() as this =
    inherit Window()

    let c1 = new ucData()   
    let c2 = new ucData()   

    do
        this.Width <- 300.
        this.Height <- 300.

        let sp = new StackPanel()

        c1.Description <- "Pippo"
        c1.Data <- 1200.
        c1.UnitOfMeasure <- "mm"
        c1.BorderThickness <- new Thickness(2.0)

        c2.Description <- "Pippo2"
        c2.Data <- 400.
        c2.UnitOfMeasure <- "MPa"
        c2.BorderThickness <- new Thickness(2.0)

        sp.Children.Add(c1) |> ignore
        sp.Children.Add(c2) |> ignore

        this.Content <- sp

// ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ //

let w = new TestWindow()
w.Show()