using NUnit.Framework;
using Zenject;

public class _2_ScopeTest : DiContainerTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        KlassWithId.ResetIdCounter();
    }

    [Test]
    public void _01_BindAsTransient()
    {
        // AsTransient()はResolve()ごとに新しくインスタンスが作られる
        Container.Bind<KlassWithId>().AsTransient();
        Assert.AreEqual(0, Container.Resolve<KlassWithId>().id);
        Assert.AreEqual(1, Container.Resolve<KlassWithId>().id);
        Assert.AreEqual(2, Container.Resolve<KlassWithId>().id);
    }

    [Test]
    public void _02_BindAsCached()
    {
        // AsCached()は最初のResolve()時にインスタンスを生成&キャッシュして以降のResolve()でも同じインスタンスを返す
        Container.Bind<KlassWithId>().AsCached();
        Assert.AreEqual(0, Container.Resolve<KlassWithId>().id);
        Assert.AreEqual(0, Container.Resolve<KlassWithId>().id);

        // バインドし直す（キャッシュクリアされる）
        Container.Unbind<KlassWithId>();
        Container.Bind<KlassWithId>().AsCached();

        // 新しいインスタンスがキャッシュされ、バインド前とは異なるインスタンスが得られる（id:1）
        Assert.AreEqual(1, Container.Resolve<KlassWithId>().id);
        Assert.AreEqual(1, Container.Resolve<KlassWithId>().id);

        // Rebind()はUnbind()->Bind()と同じ
        Container.Rebind<KlassWithId>().AsCached();

        // Rebind()前とは異なるインスタンスが得られる（id:2）
        Assert.AreEqual(2, Container.Resolve<KlassWithId>().id);
        Assert.AreEqual(2, Container.Resolve<KlassWithId>().id);
    }

    [Test]
    public void _03_BindAsCachedMultiple()
    {
        // WARNING: AsCached()による複数バインドはエラーにならない（インスタンスがユニークであることは保証されない）
        Container.Bind<KlassWithId>().AsCached();
        Container.Bind<KlassWithId>().AsCached();
        Container.Bind<KlassWithId>().AsCached();

        // ResolveAll()はそれぞれ異なるインスタンスを返す（id:1,2,3）
        var list = Container.ResolveAll<KlassWithId>();
        Assert.AreEqual(list[0].id, 0);
        Assert.AreEqual(list[1].id, 1);
        Assert.AreEqual(list[2].id, 2);

        // ２回目以降のResolveAll()はそれぞれキャッシュされたインスタンスを返す（id:1,2,3）
        list = Container.ResolveAll<KlassWithId>();
        Assert.AreEqual(list[0].id, 0);
        Assert.AreEqual(list[1].id, 1);
        Assert.AreEqual(list[2].id, 2);

        // 複数バインドしているのでResolve()はエラー
        AssertThrows<ZenjectException>(() => Container.Resolve<KlassWithId>());
        // -> Found multiple matches when only one was expected for type 'KlassWithId'.
    }

    [Test]
    public void _04_BindAsSingle()
    {
        // AsSingle()は何度Resolve()してもキャッシュされたインスタンスを返す
        Container.Bind<KlassWithId>().AsSingle();
        for (var i = 0; i < 5; i++)
        {
            Assert.AreEqual(0, Container.Resolve<KlassWithId>().id);
        }
    }

    [Test]
    public void _05_BindAsSingle3Times()
    {
        // WARNING: 同じ型に対する２連続AsSingle()はエラーにならない
        Container.Bind<KlassWithId>().AsSingle();
        Container.Bind<KlassWithId>().AsSingle();

        // 次にFlushBindings()が呼ばれるタイミングでエラー
        AssertThrows<ZenjectException>(() => Container.Bind<KlassWithId>().AsSingle());
        // -> Assert hit! Attempted to use AsSingle multiple times for type 'KlassWithId'.
        //    As of Zenject 6+, AsSingle as can no longer be used for the same type across different bindings.
        //    See the upgrade guide for details.

        // 一度エラーになったらResolve()はエラーにならない
        Assert.AreEqual(0, Container.Resolve<KlassWithId>().id);
        Assert.AreEqual(0, Container.Resolve<KlassWithId>().id);
        Assert.AreEqual(0, Container.Resolve<KlassWithId>().id);
    }

    [Test]
    public void _06_BindAsSingle2TimesAndResolve()
    {
        // WARNING: 同じ型に対する２連続AsSingle()はエラーにならない
        Container.Bind<KlassWithId>().AsSingle();
        Container.Bind<KlassWithId>().AsSingle();

        // 次にFlushBindings()が呼ばれるタイミングでエラー
        AssertThrows<ZenjectException>(() => Container.Resolve<KlassWithId>());
        // -> Assert hit! Attempted to use AsSingle multiple times for type 'KlassWithId'.
        //    As of Zenject 6+, AsSingle as can no longer be used for the same type across different bindings.
        //    See the upgrade guide for details.

        // 一度エラーになったら次からはエラーにならない
        Assert.AreEqual(0, Container.Resolve<KlassWithId>().id);
        Assert.AreEqual(0, Container.Resolve<KlassWithId>().id);
        Assert.AreEqual(0, Container.Resolve<KlassWithId>().id);
    }

    [Test]
    public void _07_BindAsSingle2TimesAndResolveAll()
    {
        // WARNING: 同じ型に対する２連続AsSingle()はエラーにならない
        Container.Bind<KlassWithId>().AsSingle();
        Container.Bind<KlassWithId>().AsSingle();

        // 次にFlushBindings()が呼ばれるタイミングでエラー
        AssertThrows<ZenjectException>(() => Container.ResolveAll<KlassWithId>());
        // -> Assert hit! Attempted to use AsSingle multiple times for type 'KlassWithId'.
        //    As of Zenject 6+, AsSingle as can no longer be used for the same type across different bindings.
        //    See the upgrade guide for details.

        // 一度エラーになったら次からはエラーにならない
        Assert.AreEqual(1, Container.ResolveAll<KlassWithId>().Count);
        Assert.AreEqual(1, Container.ResolveAll<KlassWithId>().Count);
        Assert.AreEqual(1, Container.ResolveAll<KlassWithId>().Count);
    }

    [Test]
    public void _08_BindAsSingleMultipleWithUnbind()
    {
        Container.Bind<KlassWithId>().AsSingle();
        Container.Unbind<KlassWithId>();

        // Unbind()したのでResolve()でエラー
        AssertThrows<ZenjectException>(() => Container.Resolve<KlassWithId>());
        // -> Unable to resolve 'KlassWithId'.

        // バインドし直してからのResolve()もエラー
        Container.Bind<KlassWithId>().AsSingle();
        AssertThrows<ZenjectException>(() => Container.Resolve<KlassWithId>());
        // -> Assert hit! Attempted to use AsSingle multiple times for type 'KlassWithId'.
        //    As of Zenject 6+, AsSingle as can no longer be used for the same type across different bindings.
        //    See the upgrade guide for details.
    }
}