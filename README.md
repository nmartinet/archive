# Archive

Old code, snippets, projects. 

# Descriptions
Descriptions/notes/reminders about things included here.
Organised by language for now.

## wat 
wat folder are misc. things. I'm not too sure what they do, if they're needed. I shouldn't name a folder '?', so wat'll do.

## examples
What are most probably examples. Or sometimes I can be surprised and take my code for an example if it doesn't seem bad enough :)
# FSahrp
Fun stuff. Fucntional with a nice intergration w/ .net and OO.
I initially though that it could be usefull for excel interop. The .net/openoffice/?? (need to find the what that's called again) seems to wokr fairly well and f#'s type system could really be usefull for managing all the different types of numbers that can be in a spreadsheet ($, time, months, people, debit, credit, etc...)
The interop could also ahve been used as a replacement for the vba editor. Finally giving modernish features when writting vba - code folding, tabs, panes, etc... - thanks to avalon edit.

A bunch of stuff is breaking. Maybe the wpf stuff changed, but i'm pretty sure that some ui/wpf was running at some point

## Running
Using vscode - easier than using the full vs studio.
Follow the instructions [here](http://fsharp.org/use/windows/)
Pretty easy. adds function type annotations in code, and the run command works well. Not sure if the debug part can be used.
## Notes
- Use `paket` to manage dependencies.
- vscode extensions work well
- code looks fun with fira code and ligature - arrows and triangles :)
- 

## Scratch
Some radom files/scratch pad/copy pasted stuff that isn't even runnable.

## Scaffold
A script to scaffold a script. Dowloads packet, creates the dependencies, command to update the dependencies. Not sure how usefull it still is. I wrote it before vs code was out? It was to be able to write scripts and run them without using vs studio. Updated the paket url, should probably get that dynamically somehow. Surprised it still works.
- The install command adds the dependency in the `paket.dependencies` and also adds the requirement in a `deps.fsx` file so that the main script simply needs to require the `deps.fsx` file without having to manually add the requirement in the file. 

### Scaffold Folder
A scaffold folder with more stuff in it. dlls and the `fsi.exe` interpreter. A fully portable f# env. ??

## Pomodoro
Trying to create a pomodoro timer. Mainly to test some weird declarative ui type thing, and eml-ish state managemnt. Shoudl try to get it to actually work... how hard can it be? :)
### Temps
Bunch of temp files/exps all pointing to the same result. Will have to go back and clean all this up. Proably should have been a bit better at source control, or at least put comments.

## browser
Trying to get an embeded browser runnig.

## rndWiki
A wiki walk thing. From target page, go to ?? by alway picking the first link in the page.

## UIBuilder
Declarative ui builder thing... Again some random copy pasted part. Will need to fix it up and actually create a real package at some point

## Utils
some reflection utilities.

## Temps
lots'o temps...


