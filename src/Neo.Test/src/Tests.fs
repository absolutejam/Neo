module Neo.Test.Tests

open Neo
open System
open Expecto
open Neo4j.Driver

Expect.defaultDiffPrinter <- Diff.colourisedDiff

let driver = TestConstants.driver

type WidgetColour =
    Red | Blue | Green
    override this.ToString () =
        match this with
        | Red   -> "red"
        | Blue  -> "blue"
        | Green -> "green"

    static member Parse (x: string) =
        match x.ToLower () with
        | "red"   -> Red
        | "blue"  -> Blue
        | "green" -> Green
        | other   -> failwith $"{other} is not a vailid {nameof WidgetColour}"

type SubComponent =
    {
        Id:   Guid
        Name: string
    }

    static member OfNodeProperties (props: EntityProperties) =
        {
            Id   = props.string "Id" |> Guid.Parse
            Name = props.string "Name"
        }

    member this.ToNodeMap () =
        Map [
            nameof this.Id,   box (string this.Id)
            nameof this.Name, box this.Name
        ]


type Widget =
    {
        Id:            Guid
        Name:          string
        BatchNumber:   int64
        DateShipped:   DateTime option
        Colour:        WidgetColour
        SubComponents: SubComponent list
    }
    static member OfNodeProperties (props: EntityProperties) =
        {
            Id            = props.string "Id" |> Guid.Parse
            Name          = props.string "Name"
            BatchNumber   = props.int "BatchNumber"
            DateShipped   = props.dateTimeOrNone "DateShipped"
            Colour        = props.string "Colour" |> WidgetColour.Parse
            SubComponents = []
        }


module Widgets =
    let create (runner: #IAsyncQueryRunner) (widget: Widget) =
        """
        CREATE (widget:Widget {
            Id:          $id,
            Name:        $name,
            BatchNumber: $batchNumber,
            DateShipped: $dateShipped,
            Colour:      $colour
        })
        WITH widget
        UNWIND $subs as sub
        CREATE (s:SubComponent)
        SET s = sub
        """
        |> Neo.contextFromQuery
        |> Neo.parameters [
            "id",          NeoValue.String (string widget.Id)
            "name",        NeoValue.String widget.Name
            "batchNumber", NeoValue.Integer widget.BatchNumber
            "dateShipped", (match widget.DateShipped with Some x -> NeoValue.Date x | _ -> NeoValue.Null)
            "colour",      NeoValue.String (string widget.Colour)
            "subs",        NeoValue.MapList [ for sub in widget.SubComponents -> sub.ToNodeMap () ]
        ]
        |> Neo.executeQuery runner Neo.returnSummary



    let find (runner: #IAsyncQueryRunner) (id: Guid) =
        """
        MATCH (widget:Widget { Id: $id })
        OPTIONAL MATCH (widget)-[:HAS_SUBCOMPONENT]->(sub:SubComponent)
        RETURN widget, collect(sub) as subcomponents
        """
        |> Neo.contextFromQuery
        |> Neo.parameters [ "id", NeoValue.String (string id) ]
        |> Neo.executeQuery runner (
            Neo.returnSingleWithSummary (fun returns ->
                let widgetNode        = returns.nodeProperties "widget"
                let subComponentNodes = returns.nodePropertiesList "subcomponents"
                let subComponents     = [ for sub in subComponentNodes -> SubComponent.OfNodeProperties sub ]
                { Widget.OfNodeProperties widgetNode with SubComponents = subComponents }
            )
        )

[<Tests>]
let tests =
    testList "reading & writing" [
        testCaseAsync "should persist a Widget with SubComponents and read then back successfully" <| async {
            use session = driver.AsyncSession ()

            let widgetId = Guid.NewGuid ()
            let subComponents = [
                for i in 0..9 ->
                    {
                        Id   = Guid.NewGuid ()
                        Name = $"subcomponent_{i}"
                    }
            ]

            let widget =
                {
                    Id            = widgetId
                    Name          = "Flux capacitor"
                    BatchNumber   = 69L
                    DateShipped   = None
                    Colour        = WidgetColour.Red
                    SubComponents = subComponents
                }


            let! createWidgetResult =
                Widgets.create session widget |> Async.AwaitTask

            "all nodes are created"
            |> Expect.equal createWidgetResult.Counters.NodesCreated (subComponents.Length + 1)

            let! findWidgetResult =
                Widgets.find session widgetId |> Async.AwaitTask

            let returned, summary =
                "should run query successfully"
                |> Expect.wantOk findWidgetResult

            "id should match"
            |> Expect.equal returned.Id widget.Id

            "description should match"
            |> Expect.equal returned.Name widget.Name
        }
    ]