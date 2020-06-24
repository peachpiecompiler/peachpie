<?php
namespace spl\AppendIterator_001;

// Examples from https://www.php.net/manual/en/appenditerator.construct.php
function test() {
  $pizzas   = new \ArrayIterator(array('Margarita', 'Siciliana', 'Hawaii'));
  $toppings = new \ArrayIterator(array('Cheese', 'Anchovies', 'Olives', 'Pineapple', 'Ham'));

  $appendIterator = new \AppendIterator;
  $appendIterator->append($pizzas);
  $appendIterator->append($toppings);

  foreach ($appendIterator as $key => $item) {
    echo $appendIterator->getIteratorIndex() . ' => ' . $key . ' => ' . $item . "\n";
  }
}

test();
