# Delta's UdonSharp PreProcessor Framework

## Installation
Clone the repository into your project's Assets folder



# Usage
Let's say you want to make a PreProcessor that replaces any text saying "Heya!" with "Whaaa"

We'll start off by creating a static class, let's call it TextReplacePreProcessor.
Inside said class we'll want to create a Static Parse function, but you can name it whatever you want.

The Parse function will take a string, a PPInfo variable, and it'll return a string, that string is the source code of the U# program you want to parse. \
PPInfo is in the USPPPatcher namespace.
```c#
public static class TextReplacePreProcessor {
    private static string Parse(string program, PPInfo info)
    { 
        return program.Replace("\"Heya!\"", "\"Whaaa\"");
    }
}
```

Now that we have a parse function we'll want to subscribe it to the Patcher so that our parse function gets run for every U# program.

You'll want to call the Subscribe function in the PPHandler class, the first parameter is your Parse function, second is the Priority of your PreProcessor. \
this determines in which order the PreProcessors are run (Higher is earlier). \
and the last Parameter is the name of your PreProcessor, currently this is only used for logging.

You'd do this by creating another static function inside your class. \
You'd probably want to use [InitializeOnLoadMethod] so that your subcribe function gets called whenever your script is loaded. \
PPHandler is in the USPPPatcher namespace.
```c#
public static class TextReplacePreProcessor {
    private static string Parse(string program, PPInfo info)
    { 
        return program.Replace("\"Heya!\"", "\"Whaaa\"");
    }
    
    [InitializeOnLoadMethod]
    private static void Subscribe()
    {
        PPHandler.Subscribe(Parse, 1, "Example Text Replacer");
    }
}
```

For more examples have a look at the Project(s) below

## Projects using USPPPatcher
- [USPPNet](https://github.com/DeltaNeverUsed/USPPNet)
  - Adds RPC calls with parameters to U#
