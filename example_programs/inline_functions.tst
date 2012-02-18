///////////////////////////////////////////////////////////////////////
///////////////           INLINE FUNCTIONS              ///////////////
///////////////////////////////////////////////////////////////////////

// These could be pretty dangerous, so in the next version of Toast I might make it so you call them
// a bit like this: inline_func.execute()  (assuming that I make the next version object oriented)

let my_func(func) =
  let x = 10
  func()
  print(x)
end

let inline_func = begin
  let x = 20
end

my_func(inline_func) // will print 20

let my_result = inline_func() // can also be used as a regular function
print(x) // but it has been inlined into the main program, so now x has been declared
