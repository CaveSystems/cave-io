using System;

namespace Cave.IO;

/// <summary>
/// Delegate type for the fast array getter method, which takes an array and an index and returns the element at that index as an object (boxed if it's a
/// value type).
/// </summary>
public delegate object? ArrayGetter(Array array, int index);
