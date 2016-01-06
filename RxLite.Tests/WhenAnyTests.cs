using System.Reactive.Subjects;
using NUnit.Framework;

namespace RxLite.Tests
{
    [TestFixture]
    public class WhenAnyTests
    {
        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("abc", true)]
        public void Simple_WhenAny_CanExecute(string paramValue, bool expectedCanExecute)
        {
            var vm = new WhenAnyViewModel();
            vm.ParamForWhenAny = paramValue;
            Assert.AreEqual(expectedCanExecute, vm.CommandWithWhenAny.CanExecute(null));
        }

        [TestCase(null, null, false)]
        [TestCase("", null, false)]
        [TestCase(null, "", false)]
        [TestCase("abc", null, false)]
        [TestCase("abc", "", false)]
        [TestCase(null, "", false)]
        [TestCase(null, "abc", false)]
        [TestCase("abc", "def", true)]
        public void Simple_MultipleWhenAny_CanExecute(string firstParamValue, string secondParamValue,
            bool expectedCanExecute)
        {
            var vm = new WhenAnyViewModel();
            vm.ParamForWhenAny = firstParamValue;
            vm.ParamForWhenAny2 = secondParamValue;
            Assert.AreEqual(expectedCanExecute, vm.CommandWithMultipleWhenAny.CanExecute(null));
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("abc", true)]
        public void Simple_WhenAnyValue_CanExecute(string paramValue, bool expectedCanExecute)
        {
            var vm = new WhenAnyViewModel();
            vm.ParamForWhenAnyValue = paramValue;
            Assert.AreEqual(expectedCanExecute, vm.CommandWithWhenAnyValue.CanExecute(null));
        }

        [TestCase(null, null, false)]
        [TestCase("", null, false)]
        [TestCase(null, "", false)]
        [TestCase("abc", null, false)]
        [TestCase("abc", "", false)]
        [TestCase(null, "", false)]
        [TestCase(null, "abc", false)]
        [TestCase("abc", "def", true)]
        public void Simple_MultipleWhenAnyValue_CanExecute(string firstParamValue, string secondParamValue,
            bool expectedCanExecute)
        {
            var vm = new WhenAnyViewModel();
            vm.ParamForWhenAnyValue = firstParamValue;
            vm.ParamForWhenAnyValue2 = secondParamValue;
            Assert.AreEqual(expectedCanExecute, vm.CommandWithMultipleWhenAnyValue.CanExecute(null));
        }

        private class WhenAnyViewModel : ReactiveObject
        {
            private Subject<bool> _paramForObservable;

            private string _paramForWhenAny;

            private string _paramForWhenAny2;

            private string _paramForWhenAnyValue;

            private string _paramForWhenAnyValue2;

            public WhenAnyViewModel()
            {
                var canExecute = this.WhenAny(vm => vm.ParamForWhenAny, x => !string.IsNullOrWhiteSpace(x.Value));
                CommandWithWhenAny = ReactiveCommand.Create(canExecute);

                var canExecute2 = this.WhenAnyValue<WhenAnyViewModel, bool, string>(vm => vm.ParamForWhenAnyValue,
                    x => !string.IsNullOrWhiteSpace(x));
                CommandWithWhenAnyValue = ReactiveCommand.Create(canExecute2);

                ParamForObservable = new Subject<bool>();
                CommandWithObservable = ReactiveCommand.Create(ParamForObservable);

                var canExecute3 = this.WhenAny(vm => vm.ParamForWhenAny, vm => vm.ParamForWhenAny2,
                    (first, second) =>
                        !string.IsNullOrWhiteSpace(first.Value) && !string.IsNullOrWhiteSpace(second.Value));
                CommandWithMultipleWhenAny = ReactiveCommand.Create(canExecute3);

                var canExecute4 = this.WhenAnyValue(vm => vm.ParamForWhenAnyValue, vm => vm.ParamForWhenAnyValue2,
                    (first, second) => !string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(second));
                CommandWithMultipleWhenAnyValue = ReactiveCommand.Create(canExecute4);
            }

            public string ParamForWhenAny
            {
                get { return _paramForWhenAny; }
                set { this.RaiseAndSetIfChanged(ref _paramForWhenAny, value); }
            }

            public string ParamForWhenAny2
            {
                get { return _paramForWhenAny2; }
                set { this.RaiseAndSetIfChanged(ref _paramForWhenAny2, value); }
            }

            public string ParamForWhenAnyValue
            {
                get { return _paramForWhenAnyValue; }
                set { this.RaiseAndSetIfChanged(ref _paramForWhenAnyValue, value); }
            }

            public string ParamForWhenAnyValue2
            {
                get { return _paramForWhenAnyValue2; }
                set { this.RaiseAndSetIfChanged(ref _paramForWhenAnyValue2, value); }
            }

            public Subject<bool> ParamForObservable
            {
                get { return _paramForObservable; }
                set { this.RaiseAndSetIfChanged(ref _paramForObservable, value); }
            }

            public ReactiveCommand<object> CommandWithWhenAny { get; }

            public ReactiveCommand<object> CommandWithMultipleWhenAny { get; }

            public ReactiveCommand<object> CommandWithWhenAnyValue { get; }

            public ReactiveCommand<object> CommandWithMultipleWhenAnyValue { get; }

            public ReactiveCommand<object> CommandWithObservable { get; }
        }

        [Test]
        public void Simple_Observable_CanExecute()
        {
            var vm = new WhenAnyViewModel();

            vm.ParamForObservable.OnNext(false);
            Assert.IsFalse(vm.CommandWithObservable.CanExecute(null));

            vm.ParamForObservable.OnNext(true);
            Assert.IsTrue(vm.CommandWithObservable.CanExecute(null));
        }
    }
}