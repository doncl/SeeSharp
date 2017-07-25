# Sid is a very lightweight C# RESTful framework.
It works on both Mono and .NET, with Mono/Linux being the primary intended target.
To create an endpoint:

0.  Create a subclass of SidModule.
1.  Give your class a RootPath attribute (see TestModule.cs).
2.  Add method/endpoint(s).  Do NOT declare them as public, or Sid won't use them.
3.  Use the various attributes (Get, Put, Post, etc.) implemented in the Declarations subdirectory to 
    fully describe a route for your endpoint(s).  
4.  Your endpoint(s) can take arguments as 
     a. PathAttributes (the argument is on the path - see GetTestObject() for a simple example).
     b. QueryAttributes (the argument is on the querystring).  Just declare a parameter with [Query] in 
        your method signature.
     c. BindAttributes - these are POST or PUT bodies, just declare a [Bind] parameter on your function that
        is some C# class, and the plumbing will deserialize it for you.   If it's a flat list of key-value
        pairs, it can also be called from the querystring, and the system will use it.
     d. HeaderAttributes - again, declare it in your method signature, and the system will look for an HTTP
        header with that name.
     e. CookieAttributes - hopefully this is self-explanatory by now.

5. To run things, create an instance of the WebService object, and call Run().  It's asynchronous, so you'll
   need to arrange for you main thread not to die and take everything down with it (Console.ReadKey() is the
   simplest, crudest way to do this), or sleep in a loop, waiting for interrupt, however you like. 
   
