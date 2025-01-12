using System;

namespace Log2ui.Collections.Observables;

public class NotEmittedException() : Exception("The Observable has not emitted a value yet.");
