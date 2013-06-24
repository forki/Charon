﻿#load "Index.fs"
#load "DecisionTree.fs"

open Charon
open Charon.DecisionTree
open System
open System.IO
open System.Diagnostics

#time

// Test on the Nursery dataset from UC Irvine ML Repository:
// http://archive.ics.uci.edu/ml/machine-learning-databases/nursery/
// Path to the data file:
let desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
let nurseryPath = desktopPath + @"\nursery.txt"

// Create a synthetic, random dataset and run a tree on it
let test (size: int) (feat: int) (outcomes: int) =

    let rng = System.Random()

    let labels = [| for i in 0 .. size -> rng.Next(0, outcomes + 1) |] |> prepare
    let data = [| for f in 1 .. feat -> [| for i in 0 .. size -> rng.Next(0, outcomes + 1) |] |> prepare |]

    let dataset = labels, data

    let indexes = [ 0 .. size ]
    let features = [ 0 .. (feat - 1) ] |> Set.ofList
    let minLeaf = 5

    printfn "Initialized"

    let timer = Stopwatch()
    
    timer.Restart()

    let tree = build dataset indexes features any minLeaf

    timer.Stop()

    printfn "Tree building: %i ms" timer.ElapsedMilliseconds

// Test on the Nursery dataset (see comments on top of file)
let nursery () =

    let timer = Stopwatch()
       
    let data = 
        File.ReadAllLines(nurseryPath)
        |> Array.map (fun line -> line.Split(','))
        |> Array.filter (fun line -> Array.length line = 9)

    timer.Restart()

    let features =
        [| data |> extract (fun line -> line.[8]); // labels
           data |> extract (fun line -> line.[0]);
           data |> extract (fun line -> line.[1]);
           data |> extract (fun line -> line.[2]);
           data |> extract (fun line -> line.[3]);
           data |> extract (fun line -> line.[4]);
           data |> extract (fun line -> line.[5]);
           data |> extract (fun line -> line.[6]);
           data |> extract (fun line -> line.[7]); |]
            
    timer.Stop()
    printfn "Features analysis: %i ms" timer.ElapsedMilliseconds

    timer.Restart()

    let transform = converter features
    let trainingSet = prepareTraining data transform

    timer.Stop()

    printfn "Data preparation: %i ms" timer.ElapsedMilliseconds

    timer.Restart()
    let minLeaf = 5
    let classifier = ID3Classifier trainingSet [ 0.. (data |> Array.length) - 1 ] minLeaf
    timer.Stop()

    printfn "Tree building: %i ms" timer.ElapsedMilliseconds

    printfn "Forecast evaluation"
    let correct = 
        data
        |> Array.map (snd transform) 
        |> Array.averageBy (fun x -> 
            let lbl, obs = x
            if lbl = (classifier obs) then 1. else 0.)
    printfn "Correct: %.3f" correct

// Test on the Nursery dataset (see comments on top of file)
let nurseryForest () =

    let timer = Stopwatch()

    let data = 
        File.ReadAllLines(nurseryPath)
        |> Array.map (fun line -> line.Split(','))
        |> Array.filter (fun line -> Array.length line = 9)

    printfn "Training set size: %i" (Array.length data)

    timer.Restart()

    let features =
        [| data |> extract (fun line -> line.[8]); // labels
           data |> extract (fun line -> line.[0]);
           data |> extract (fun line -> line.[1]);
           data |> extract (fun line -> line.[2]);
           data |> extract (fun line -> line.[3]);
           data |> extract (fun line -> line.[4]);
           data |> extract (fun line -> line.[5]);
           data |> extract (fun line -> line.[6]);
           data |> extract (fun line -> line.[7]); |]
            
    timer.Stop()
    printfn "Features analysis: %i ms" timer.ElapsedMilliseconds

    timer.Restart()

    let transform = converter features
    let trainingSet = prepareTraining data transform

    timer.Stop()

    printfn "Data preparation: %i ms" timer.ElapsedMilliseconds

    timer.Restart()
    let minLeaf = 5
    let bagging = 0.75
    let iters = 50

    let forest = forest trainingSet [ 0.. (data |> Array.length) - 1 ] minLeaf bagging iters
    timer.Stop()

    printfn "Forest building: %i ms" timer.ElapsedMilliseconds

    printfn "Forecast evaluation"
    let correct = 
        data
        |> Array.map (snd transform) 
        |> Array.averageBy (fun x -> 
            let lbl, obs = x
            if lbl = (forestDecide forest obs) then 1. else 0.)
    printfn "Correct: %.3f" correct