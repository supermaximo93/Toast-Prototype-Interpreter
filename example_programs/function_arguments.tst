///////////////////////////////////////////////////////////////////////
///////////////          FUNCTION ARGUMENTS             ///////////////
///////////////////////////////////////////////////////////////////////

let iterate_to_10(func) =
  let x = 1
  while x <= 10,
    func(x)
    let x = x + 1
  end
end

iterate_to_10(print) // prints numbers 1 to 10

let print_square(x) = print(x * x)
iterate_to_10(print_square) // prints squares of numbers 1 to 10

let to_real(num) = 1.0 * num // need this to convert from int to real (1 * 1.2 = 1 but 1.0 * 1.2 = 1.2)

let inline_function = begin
  let x = to_real(x) - 0.5
  print(x)
end

iterate_to_10(inline_function) // prints 0.5 - 9.5 in increments of 0.5
