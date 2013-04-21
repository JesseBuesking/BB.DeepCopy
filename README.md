## This is an add-in for [Fody](https://github.com/Fody/Fody/)

Adds deep copying to objects.

[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage)

## The problem this addresses

See [this answer](http://stackoverflow.com/a/129395/435460) to a [stack overflow](http://www.stackoverflow.com) on deep copying objects in .Net for reference.

The simplest and standard way to copy objects is to create an extension method like the one in the answer to the [stack overflow](http://www.stackoverflow.com) question above. This is the best solution to achieve deep copying of objects with the least amount of effort, but it comes at a cost: it isn't a very fast operation.

If you want to have a solution that performs well, your best option is to add a method to your object that performs the deep copying explicitly, but now you have to write the logic to perform the deep copy by hand. This is a tedious task that adds more code that you must now maintain. In addition to this, there are situations where you have to modify existing objects to expose their private members in order to copy them. You might even need to add default constructors to some of your objects now to support the deep copying.

This can become a bit of a mess very quickly.

## How to use this

Most programmers are probably like me and chose to stick with the binary serialization extension method supplied by the answer in the SO question from above. It's by far the simplest solution, and in most cases the performance hit you take by using it is insignificant.

If you're like me, then you're in a great position! All you have to do is include [BB.DeepCopy](https://github.com/JesseBuesking/BB.DeepCopy/) in the project that contains your deep copy extension, and add a `[DeepCopyMethod]` attribute to your current deep copying extension method. Done.

For everyone else, add the extension method and apply the `[DeepCopyMethod]` to it. You don't even need the binary serialization logic in the method, it could simply return null (although it's nice to have the binary serialization logic to fall back to in cases where [BB.DeepCopy](https://github.com/JesseBuesking/BB.DeepCopy/) can't add a deep copying method).

## What this does

[BB.DeepCopy](https://github.com/JesseBuesking/BB.DeepCopy/) will add a deep copy method to each of your objects (abstract base classes and interfaces included). It will then replace all calls you had in your code to the deep copy extension method with calls to the new deep copy method that's been added to the object.

## Performance remarks

* For fields on objects, this performs just as well as a hand-copy.
* For auto-implemented properties (e.g. `public int MyProperty { get; set; }`, this will outperform a hand copy.
* For properties where you have access to the backing field, this will perform just as well as a hand-copy.

(Check out the [performance tests in the test project](https://github.com/JesseBuesking/BB.DeepCopy/blob/master/Tests/Performance/PerformanceTests.cs)).

## Bugs/Issues

Please report any bugs/issues found using the issue tracking [here on GitHub](https://github.com/JesseBuesking/BB.DeepCopy/issues).


