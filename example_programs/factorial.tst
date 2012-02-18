///////////////////////////////////////////////////////////////////////
///////////////               FACTORIAL                 ///////////////
///////////////////////////////////////////////////////////////////////

let iterative_factorial(x) =
  if x <= 1, exit(1)
  let result = x
  while x > 1,
    let x = x - 1
    let result = result * x
  end
  result
end

let recursive_factorial(x) =
  if x <= 1,
    1
  else
    x * recursive_factorial(x - 1)
  end
end

let single_line_factorial(x) = if x <= 1, 1 else x * single_line_factorial(x - 1)
