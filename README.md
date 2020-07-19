# Opinionated-csharp-todos

![Check](
https://github.com/mristin/opinionated-csharp-todos/workflows/Check/badge.svg
) [![Coverage Status](
https://coveralls.io/repos/github/mristin/opinionated-csharp-todos/badge.svg)](
https://coveralls.io/github/mristin/opinionated-csharp-todos
) [![Nuget](
https://img.shields.io/nuget/v/OpinionatedCsharpTodos)](
https://www.nuget.org/packages/OpinionatedCsharpTodos
)

Opinionated-csharp-todos inspects that your TODO comments follow a pattern.

## Motivation

It is quite messy to have a variety of TODO comment styles in your code:
* New developers are confused as which style they should use. 

  For example, if you mix case, is it `// todo` or `// Todo` or `// TODO`?
  
  &rightarrow; You want a uniform style throught your code base.
  
* The set of the prefixes is not well-defined. Certain tools support by default
  only a limited set of prefixes. 
  
  For example, Microsoft Visual Studio supports by default  
  `HACK`, `TODO`, `UNDONE` and `UnresolvedMergeConflict` (see [this page](
  https://docs.microsoft.com/en-us/visualstudio/ide/using-the-task-list?view=vs-2019
  )). On the other hand, JetBrains Rider supports by default `TODO` and `BUG`
  (see [this page](
  https://www.jetbrains.com/help/rider/Navigation_and_Search__Navigating_Between_To_do_Items.html#patterns
  )). Both tools support adding custom prefixes (*a.k.a*, *tags* or *tokens*).
  
  &rightarrow; You want to have a separate IDE-independent tool which ensures 
  that the established convention is followed in the code base. Best if done
  in continuous integration. 
  
* Bare TODOs can be daunting in larger code bases with multiple contributors. 
  Single `// TODO` without further information such as the author and the date
  of the comment can be confusing or even overwhelming, especially to new 
  developers.
  
  For example, if you have to work on a part of the code base with a highly
  relevant TODO, how are you supposed to contact the person who left it there
  to gain further insights and background knowledge?
  
  While using `git blame` might a be a solution up to a certain degree,
  this does not work whenever the copy/pasted code includes the TODOs.
  This situation often happens in refactorings when the "refactorer" is not the 
  author of the code. 
  
  &rightarrow; You want the information about the author and the time stamp
  included in the TODOs.
  This additional information should be structured in an uniform manner. 

* Structured information is necessary for further processing. Bare TODOs
  bar that possibility.
  
  For example, imagine you would like to create an additional tool
  to analyze the TODOs and create Github issues automatically.
  You might want to include the information such as timestamp, due date, author
  and issue tag in the TODO. 
  
  &rightarrow; Though complex examination of the comment structure is 
  out-of-scope for Opinionated-csharp-todos, it is a good idea to 
  check that the TODO comments at least match the expected patterns and 
  inform the developer as soon as possible if some of the comments do not match.  

## Related Tools

**IDEs and extensions.**
While popular IDEs support the TODO comments themselves (*e.g.*, 
[Task lists in Visual Studio](
https://docs.microsoft.com/en-us/visualstudio/ide/using-the-task-list?view=vs-2019
)) and also provide ground for many extensions (*e.g.*, [this extension](
https://marketplace.visualstudio.com/items?itemName=TzachOvadia.todo-list
) and [that extension](
https://marketplace.visualstudio.com/items?itemName=OmarRwemi.BetterComments
)), these tools are difficult or impossible to put into
the continuous integration since they lack a command-line interface.

At best, you could use them to re-format and inspect the TODOs *manually*.

**TODOs-as-a-service**. There is a service, [Tickgit](
https://www.tickgit.com/), that you connect with the repository to inspect
your TODOs and extract the extra information such as the author and 
the time stamp using `git blame`. Unfortunately, this typically breaks even in
smaller teams  whenever the person refactoring is not the author of
the original code.

**Command-line tools**. We searched [nuget.org](https://www.nuget.org) when
we started developing the tool (July 2020). There were only a few related
tools, none of which could enforce arbitrary patterns:

* [DatedTodo](https://github.com/MartinJohns/DatedTodo/) inspects the TODOs,
  parses the due date and raises an alarm if some of the TODOs are due.
  
* [TodoCommentReporter](https://github.com/yugabe/TodoCommentReporter) is a 
  Roslyn diagnostic analyzer that reports the TODOs in your code base.
  
* [FixMe](https://github.com/otac0n/FixMe) emits the TODO comments during the 
  build. 

## Installation

Opinionated-csharp-todos is available as a dotnet tool.

Either install it globally:

```bash
dotnet tool install -g OpinionatedCsharpTodos
```

or locally (if you use tool manifest, see [this Microsoft tutorial](
https://docs.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use)):

```bash
dotnet tool install OpinionatedCsharpTodos
```

## Usage

### Inputs and Excludes

You run opinionated-csharp-todos through `dotnet`.

To obtain help:

```bash
dotnet opinionated-csharp-todos --help
```

You specify the files containing TODOs using glob patterns:

```bash
dotnet opinionated-csharp-todos --inputs "SomeProject/**/*.cs"
```

Multiple patterns are also possible *(we use '\\' for line continuation here)*:

```bash
dotnet opinionated-csharp-todos \
    --inputs "SomeProject/**/*.cs" \
        "AnotherProject/**/*.cs"
```

Sometimes you need to exclude files, *e.g.*, when your solution
contains third-party code which you do not want to scan.

You can provide the glob pattern for the files to be excluded with `--excludes`:

```bash
dotnet opinionated-csharp-todos \
    --inputs "**/*.cs" \
    --excludes "**/obj/**"
```

### Patterns

**Prefixes** are specified as regular expressions using `--prefix` . 

For example, if you only want to scan for `// TODO` and `// BUG`:

```bash
dotnet opinionated-csharp-todos \
    --inputs "**/*.cs" \
    --prefix "^TODO" "^BUG"
```

**Disallowed prefixes**. You can make opinionated-csharp-comments fail whenever 
one of the disallowed prefixes is encountered specified as regular expressions
using `--disallowed-prefix`. 

This is particularly handy if you want to include the tool in your pre-commit 
checks to make sure you do not check in, say, unfinished work.   

For example, if you want the tool to fail on `// DONT-CHECK-IN` and ` // DEBUG`:

```bash
dotnet opinionated-csharp-todos \
    --inputs "**/*.cs" \
    --disallowed-prefix "^DONT-CHECK-IN" "^DEBUG"
```

**Suffixes** are usually also required to follow the convention(s). You can
specify the patterns as regular expressions using `--suffix`.

For example, to enforce suffix to match `// TODO (mristin, 2020-07-20): ...`:

```bash
dotnet opinionated-csharp-todos \
    --inputs "**/*.cs" \
    --suffix '^ \([^)]+, [0-9]{4}-[0-9]{2}-[0-9]{2}\): '
```

We provide the **defaults** for `--prefix`, `--disallowed-prefix` and `--suffix`
which probably work in most of the cases. See the output of `--help` for 
more information.

### Report

Opinionated-csharp-todos scans the files and reports the collected TODOs to the
standard output as text. 
In cases where you would like to post-process the results, you can
save the report as a JSON file using `--report-path`.

For example:

```bash
dotnet opinionated-csharp-todos \
    --inputs "**/*.cs" \
    --report-path /tmp/some-report.json
```

If you provide `-`, the JSON report is written to the standard output:

```bash
dotnet opinionated-csharp-todos \
    --inputs "**/*.cs" \
    --report-path -
```

## Recipes

Opinionated-csharp-todos is based on matching prefixes and suffixes using 
regular expressions. This logic is quite limiting and often you would like to
further refine the checks and post-process the collected TODOs.

We provide two recipes to demonstrate how you can further use the JSON report 
downstream.

### Badges

The script [recipes/powershell/RenderBadge.ps1](
recipes/powershell/RenderBadge.ps1) uses https://shields.io to render the number
of TODOs to SVG badges. If you include this script in your continuous 
integration, you can put up the badge to let the users know the
state of the code base.

### Task List

The script [recipes/powershell/RenderTaskList.ps1](
recipes/powershell/RenderTaskList.ps1
) renders the TODOs as markdown grouped by the corresponding source files.
By specifying an URL prefix it automatically links the location of the TODOs
to the code base.

Thus you can use this task list for a rudimentary task management and navigate
the TODOs.

## Contributing

Feature requests, bug reports *etc.* are highly welcome! Please [submit
a new issue](https://github.com/mristin/opinionated-csharp-todos/issues/new).

If you want to contribute in code, please see
[CONTRIBUTING.md](CONTRIBUTING.md).

## Versioning

We follow [Semantic Versioning](http://semver.org/spec/v1.0.0.html).
The version X.Y.Z indicates:

* X is the major version (backward-incompatible w.r.t. command-line arguments),
* Y is the minor version (backward-compatible), and
* Z is the patch version (backward-compatible bug fix).
