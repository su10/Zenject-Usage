using NUnit.Framework;
using Zenject;

public class _4_BindClassAndInterfaceTest : DiContainerTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        KlassWithId.ResetIdCounter();
    }

    public interface IFoo
    {
        int id { get; }
        string name { get; }
    }

    public interface IBar
    {
    }

    public class FooBase : KlassWithId, IFoo, IBar
    {
        public virtual string name
        {
            get { return "FooBase"; }
        }
    }

    public class Foo : FooBase
    {
        public override string name
        {
            get { return "Foo"; }
        }
    }

    [Test]
    public void _01_BindBaseClass()
    {
        // 基底クラスを派生クラスにバインド
        Container.Bind<FooBase>().To<Foo>().AsTransient();

        // バインドしたクラスは解決できる（中身はバインド先クラスのインスタンス）
        FooBase fooBase = Container.Resolve<FooBase>();
        Assert.AreEqual("Foo", fooBase.name);

        // バインド先クラスは直接解決できない
        AssertThrows<ZenjectException>(() => Container.Resolve<Foo>());
        // -> Unable to resolve '_4_BindClassAndInterfaceTest.Foo'.
    }

    [Test]
    public void _02_BindInterface()
    {
        // インターフェースをその実装クラスにバインド
        Container.Bind<IFoo>().To<Foo>().AsTransient();

        // インターフェースは解決できる（中身はバインド先クラスのインスタンス）
        IFoo ifoo = Container.Resolve<IFoo>();
        Assert.AreEqual("Foo", ifoo.name);

        // バインド先クラスは直接解決できない
        AssertThrows<ZenjectException>(() => Container.Resolve<Foo>());
        // -> Unable to resolve '_4_BindClassAndInterfaceTest.Foo'.
    }

    [Test]
    public void _03_ResolveBaseClassAndInterface()
    {
        // バインドしたクラスの基底クラスおよびインターフェースは直接解決できない
        Container.Bind<Foo>().AsTransient();
        AssertThrows<ZenjectException>(() => Container.Resolve<FooBase>());
        AssertThrows<ZenjectException>(() => Container.Resolve<IFoo>());
        // -> Unable to resolve '_4_BindClassAndInterfaceTest.FooBase'.
        // -> Unable to resolve '_4_BindClassAndInterfaceTest.IFoo'.
    }

    [Test]
    public void _04_AsTransientAndResolve()
    {
        // AsTransient()でそれぞれバインド
        Container.Bind<FooBase>().To<Foo>().AsTransient();
        Container.Bind<IFoo>().To<Foo>().AsTransient();
        FooBase fooBase = Container.Resolve<FooBase>();
        IFoo ifoo = Container.Resolve<IFoo>();

        // どちらもFooのインスタンス
        Assert.AreEqual("Foo", fooBase.name);
        Assert.AreEqual("Foo", ifoo.name);

        // インスタンスは異なる
        Assert.AreEqual(0, fooBase.id);
        Assert.AreEqual(1, ifoo.id);
    }

    [Test]
    public void _05_AsCachedAndFromResolveAndResolve()
    {
        // AsCached()でバインド
        Container.Bind<Foo>().AsCached();

        // FromResolve()によってFooへのバインドは既存のバインドから解決
        Container.Bind<FooBase>().To<Foo>().FromResolve();
        Container.Bind<IFoo>().To<Foo>().FromResolve();

        Foo foo = Container.Resolve<Foo>();
        FooBase fooBase = Container.Resolve<FooBase>();
        IFoo ifoo = Container.Resolve<IFoo>();

        // 全てFooのインスタンス
        Assert.AreEqual("Foo", foo.name);
        Assert.AreEqual("Foo", fooBase.name);
        Assert.AreEqual("Foo", ifoo.name);

        // AsCached() + FromResolve() によって全て同じインスタンス
        Assert.AreEqual(0, foo.id);
        Assert.AreEqual(0, fooBase.id);
        Assert.AreEqual(0, ifoo.id);
    }

    [Test]
    public void _06_BindInterfaces()
    {
        // バインドするクラスが実装しているインターフェースを全てバインド
        Container.BindInterfacesTo<Foo>().AsCached();

        // バインド先クラスの全てのインターフェースを解決できる（AsCached()なので同じインスタンス）
        Assert.AreSame(Container.Resolve<IFoo>(), Container.Resolve<IBar>());

        // バインド先クラスは直接解決できない
        AssertThrows<ZenjectException>(() => Container.Resolve<Foo>());
        // -> Unable to resolve '_4_BindClassAndInterfaceTest.Foo'.

        // 基底クラスは直接解決できない
        AssertThrows<ZenjectException>(() => Container.Resolve<FooBase>());
        // -> Unable to resolve '_4_BindClassAndInterfaceTest.FooBase'.
    }

    [Test]
    public void _07_BindInterfacesAndSelfTo()
    {
        // クラスとそのクラスが実装しているインターフェース全てをバインド
        Container.BindInterfacesAndSelfTo<Foo>().AsCached();

        // バインドしたクラスもインターフェースも解決できる（AsCached()なので同じインスタンス）
        Assert.AreSame(Container.Resolve<Foo>(), Container.Resolve<IFoo>());
        Assert.AreSame(Container.Resolve<Foo>(), Container.Resolve<IBar>());

        // 基底クラスは直接解決できない
        AssertThrows<ZenjectException>(() => Container.Resolve<FooBase>());
        // -> Unable to resolve '_4_BindClassAndInterfaceTest.FooBase'.
    }
}