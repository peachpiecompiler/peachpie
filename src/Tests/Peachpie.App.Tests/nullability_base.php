<?php

use Peachpie\App\Tests\NullabilitySubject;

class Nullability
{
  public final function noNull(NullabilitySubject $x) {
    return $x->noNull($x);
  }

  public final function returnNull(NullabilitySubject $x) {
    return $x->returnNull($x);
  }

  public final function argNull(NullabilitySubject $x) {
    return $x->argNull($x);
  }

  public final function allNull(NullabilitySubject $x) {
    return $x->allNull($x);
  }

  public final function maybeNull(NullabilitySubject $x) {
    return $x->maybeNull();
  }

  public final function notNull(NullabilitySubject $x) {
    return $x->notNull();
  }

  public final function phpReturnNull(bool $x) {
    return $x ? new Nullability() : null;
  }

  public final function phpReturnNotNull() {
    return new Nullability();
  }

  public final function phpReturnNullExplicit(bool $x) : ?Nullability {
    return $x ? new Nullability() : null;
  }

  public final function phpReturnNotNullExplicit() : Nullability {
    return new Nullability();
  }

  public final function phpParamNull(?Nullability $x) {
    return $x;
  }
}

function get_nullability() : Nullability {
  return new Nullability();
}