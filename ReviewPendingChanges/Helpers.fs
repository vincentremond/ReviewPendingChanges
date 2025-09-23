namespace ReviewPendingChanges

[<RequireQualifiedAccess>]
module List =

    let filterResult (f: 'a -> Result<bool, 'e>) (list: 'a list) : Result<'a list, 'e list> =
        List.foldBack
            (fun item state ->
                let r = f item

                match r, state with
                | Error e, Ok _ -> Error [ e ]
                | Error e, Error xe -> Error(e :: xe)
                | Ok _, Error acc -> Error acc
                | Ok true, Ok acc -> Ok(item :: acc)
                | Ok false, Ok acc -> Ok(acc)

            )
            list
            (Ok [])
