///////////////////////////////////////////////////////////////////////
///////////////              FIBONACCI                  ///////////////
///////////////////////////////////////////////////////////////////////

let recursive_fib(n) =
  // bear in mind that functions have global scope. Next version will probably have scope with functions
  let pos_fib(n) = if n < 2, n else pos_fib(n - 1) + pos_fib(n - 2)
  
  let is_negative = n < 0
  let n = pos_fib|n|
  if is_negative, -n else n
end

let iterative_fib(n) =
  let is_negative = n < 0
  let n = |n|
  
  let first = 0
  let second = 1
  while n > 0,
    let temp = first + second
    let first = second
    let second = temp
    let n = n - 1
  end
  
  if is_negative, -first else first
end
