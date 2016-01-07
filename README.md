# RxLite
Simplified version (or subset if you wish) of the ReactiveUI framework

## Why it was build
ReactiveUI is awesome, but it looks a little bit overcomplicated for some projects. E.g. there are a lot of frameworks for cross-platform mobile app development with Xamarin which already ships with DI, MessageBus, MVVM, etc. In case if you will decide to use ReactiveUI for your project current ReactiveUI team recommendation is just to ignore things that you do not want to use. I suppose that something is wrong with me, but I do not like this approach :-)

There are no reasons to have all that framework things and keep platform-specific libraries implementations in case if you just want to use in your ViewModels such a cool things like WhenAny, ObservableAsPropertyHelper and ReactiveCommand. So I just removed a lot of code and made things work afterwards.

## Project goals
Project main goal is to keep keep limited subset of ReactiveUI features:
* this.WhenAny, this.WhenAnyValue, this.WhenAnyObservable
* ObservableAsPropertyHelper, ToProperty
* ReactiveObject, this.RaiseAndSetIfChanged
* ReactiveCommand, ReactiveAsyncCommand
* ReactiveList

Things not included in this project:
* Any IoC (especially Splat)
* Any logging frameworks
* MessageBus
* ViewLocator, IViewFor
* Any platform-specific things

## TODO
* Port TestScheduler for nicer unit tests
* Write more unit tests
* Write more samples
