using System.Collections.Generic;
using NUnit.Framework;
using Zenject;

public class _5_ArgumentsTest : DiContainerTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        KlassWithId.ResetIdCounter();
    }

    public class Fizz
    {
        public readonly int id;
        public readonly string name;

        public Fizz(int id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }

    [Test]
    public void _01_WithArguments()
    {
        // WithArgumentsでコンストラクタ引数も一緒にバインド
        const int id = 100;
        const string name = "fizz";
        Container.Bind<Fizz>().AsTransient().WithArguments(id, name);

        // インスタンスの生成でバインド時に指定したコンストラクタ引数が使われる
        var bar = Container.Resolve<Fizz>();
        Assert.AreEqual(id, bar.id);
        Assert.AreEqual(name, bar.name);

        // 実際のコンストラクタ引数の順番と違う順番でWithArguments()に渡しても正しく動く
        Container.UnbindAll();
        Container.Bind<Fizz>().AsTransient().WithArguments(name, id);
        bar = Container.Resolve<Fizz>();
        Assert.AreEqual(id, bar.id);
        Assert.AreEqual(name, bar.name);
    }

    [Test]
    public void _02_NoArguments()
    {
        Container.Bind<Fizz>().AsTransient();

        // インスタンス生成時にコンストラクタ引数が解決できないのでエラー
        AssertThrows<ZenjectException>(() => Container.Resolve<Fizz>());
        // -> Unable to resolve 'int' while building object with type '_5_ArgumentsTest.Fizz'.
    }

    [Test]
    public void _03_NoArgumentsButBind()
    {
        Container.Bind<Fizz>().AsTransient();

        // コンストラクタ引数と同じ型のインスタンスをそれぞれバインド
        const int id = 100;
        const string name = "fizz";
        Container.BindInstance(id);
        Container.BindInstance(name);

        // WARNING: コンストラクタ引数がバインドしたコンテキストから解決される！
        var bar = Container.Resolve<Fizz>();
        Assert.AreEqual(id, bar.id);
        Assert.AreEqual(name, bar.name);

        // コンストラクタ引数と型が合致するインスタンスが複数バインドされているとエラー
        Container.BindInstance("str");
        AssertThrows<ZenjectException>(() => Container.Resolve<Fizz>());
        // -> Found multiple matches when only one was expected for type 'string' while building object with type '_5_ArgumentsTest.Fizz'.
    }

    public class Buzz
    {
        public readonly List<int> numbers;

        public Buzz(List<int> numbers)
        {
            this.numbers = numbers;
        }
    }

    [Test]
    public void _04_ListArguments()
    {
        // コンストラクタ引数として使えるバインドがないのでエラー
        AssertThrows<ZenjectException>(() => Container.Resolve<Buzz>());

        Container.Bind<Buzz>().AsTransient();
        Container.BindInstance(new List<int> {10, 20, 30});
        var buzz = Container.Resolve<Buzz>();
        Assert.AreEqual(3, buzz.numbers.Count);
        Assert.AreEqual(10, buzz.numbers[0]);
        Assert.AreEqual(20, buzz.numbers[1]);
        Assert.AreEqual(30, buzz.numbers[2]);

        // コンストラクタ引数になるインスタンスをアンバインド
        Assert.AreEqual(3, Container.Resolve<List<int>>().Count);
        Container.Unbind<List<int>>();
        Assert.AreEqual(0, Container.Resolve<List<int>>().Count);

        // WARNING: クラスの解決はエラーにならない（空のListとして解決される）
        Assert.AreEqual(0, Container.Resolve<Buzz>().numbers.Count);

        // コンストラクタ引数List<T>のTをListを使わず複数バインド
        Container.BindInstance(100);
        Container.BindInstances(200, 300);
        buzz = Container.Resolve<Buzz>();

        // 複数バインドしたインスタンスがList<T>として解決される
        Assert.AreEqual(3, buzz.numbers.Count);
        Assert.AreEqual(100, buzz.numbers[0]);
        Assert.AreEqual(200, buzz.numbers[1]);
        Assert.AreEqual(300, buzz.numbers[2]);
    }
}