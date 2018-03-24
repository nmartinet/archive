
    let createMenu() =
      Menu
        |> C [MenuItem "File"
        |> C [MenuItem "Open"; MenuItem "Close"]]

      do
        t.Title <- "Editor"
        let menu = createMenu()
        let ui = G
                  |> C  [ Editor() ]
                  |> BuildUI |> AddMenu menu |> GetUI
        t.Content <- ui





        type Editor() as t =
          inherit ICSharpCode.AvalonEdit.TextEditor()

          let setHighlight f = use stream = File.OpenRead(f) in
              use reader = new XmlTextReader(stream) in
                t.SyntaxHighlighting <-
                    Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance )
          let setFont f s =
            t.FontSize <- f
            t.FontFamily <- s
          let getText() = t.Text
          let setText t = t.Text <- t
          let selection() = t.SelectedText

          do
            t.ShowLineNumbers <- true
            setFont 14. (font "Consolas")
            setHighlight "FsHighlight.xshd"
