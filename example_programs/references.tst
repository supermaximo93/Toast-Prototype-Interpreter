///////////////////////////////////////////////////////////////////////
///////////////              REFERENCES                 ///////////////
///////////////////////////////////////////////////////////////////////

let assign(ref, value) =
  let ~ref = value
end

let x = 0
assign(@x, 5)
print(x) // prints 5

let inc(num) = let ~num = ~num + 1
let dec(num) = let ~num = ~num - 1
inc(@x)
print(x) // prints 6
dec(@x)
print(x) // prints 5
