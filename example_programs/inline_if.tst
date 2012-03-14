///////////////////////////////////////////////////////////////////////
///////////////          INLINE IF AND WHILE            ///////////////
///////////////////////////////////////////////////////////////////////

let x = 1
let y = 2

let is_x_greater = if x > y, "yes, x is greater" else "no, y is greater or equal"
print(is_x_greater) // prints 'no, y is greater or equal'

let is_y_less = if y < x,
  print("y is less than x")
  "yes, x is greater"
else
  print("y is greater or equal to x")
  "no, y is greater or equal"
end
print(is_y_less) // prints 'no, y is greater or equal'

let i = 0
let x = while i < 10, let i = i + 1
print(x) // prints 10

let i = 0
let x = while i < 10,
  let i = i + 1
  if i > 5, break
end
print(x) // prints 6, the last evaluated expression (other than the condition)
