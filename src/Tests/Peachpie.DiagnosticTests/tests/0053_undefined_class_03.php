<?php

class DefinedException extends Exception {}

try {
} catch (DefinedException $exception) {
}

try {
} catch (UndefinedException/*!PHP3008!*/ $exception) {
}
