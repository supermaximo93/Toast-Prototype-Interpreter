///////////////////////////////////////////////////////////////////////
///////////////          INLINE IF AND WHILE            ///////////////
///////////////////////////////////////////////////////////////////////

let x = 1
let y = 2

let is_x_greater = if x > y, yes else no
print(is_x_greater) // prints no

let is_y_less = if y < x,
  print("y is less than x")
  yes
else
  print("y is greater or equal to x")
  no
end
print(is_y_less) // prints no

let i = 0
let x = while i < 10, let i = i + 1
print(x) // prints 10

let i = 0
let x = while i < 10,
  let i = i + 1
  if i > 5, break
end
print(x) // prints 6, the last evaluated expression (other than the condition)
