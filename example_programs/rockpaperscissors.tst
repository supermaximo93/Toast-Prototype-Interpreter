///////////////////////////////////////////////////////////////////////
///////////////          ROCK, PAPER, SCISSORS          ///////////////
///////////////////////////////////////////////////////////////////////

print("Rock, paper scissors")

let getInput() =
  print("Type 'rock', 'paper' or 'scissors'. Type 'quit' to finish")
  while yes,
    let input = read_string()
    if input = "quit", exit(-1) else
      if input = "rock" or input = "paper" or input = "scissors", break
      print("No! Type 'rock', 'paper' or 'scissors'! Type 'quit' to finish!")
    end
  end

  let playerChoice = 0

  if input = "paper", let playerChoice = 1 else if input = "scissors", let playerChoice = 2
  
  playerChoice
end

let play(playerChoice, playerScoreRef, cpuScoreRef) =
  let cpuChoice = random(3)
  if cpuChoice = 0, let cpuChoiceStr = "rock" else
    let cpuChoiceStr = (if cpuChoice = 1, "paper" else "scissors")
  end
  print()
  print("The CPU chose ", cpuChoiceStr)
  print()
  
  if playerChoice = 0,
    if cpuChoice = 0, print("Tie!") else
      if cpuChoice = 1,
        print("You lose!")
        let ~cpuScoreRef = ~cpuScoreRef + 1
      else
        print("You win!")
        let ~playerScoreRef = ~playerScoreRef + 1
      end
    end
  else
    if playerChoice = 1,
      if cpuChoice = 0,
        print("You win!")
        let ~playerScoreRef = ~playerScoreRef + 1
      else
        if cpuChoice = 1, print("Tie!") else
          let ~cpuScoreRef = ~cpuScoreRef + 1
          print("You lose!")
        end
      end
    else
      if cpuChoice = 0,
        print("You lose!")
        let ~cpuScoreRef = ~cpuScoreRef + 1
      else
        if cpuChoice = 1,
          print("You win!")
          let ~playerScoreRef = ~playerScoreRef + 1
        else print("Tie!")
      end
    end
  end
  
  print()
end

let playerScore = 0
let cpuScore = 0

while yes,
  let choice = getInput()
  if choice = -1, break
  play(choice, @playerScore, @cpuScore)
  print("Player: ", playerScore, " CPU: ", cpuScore)
  print()
end

print()
print("Thanks for playing!")