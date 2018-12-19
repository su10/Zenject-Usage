using NUnit.Framework;
using Zenject;

public class _1_BindInstanceTest : DiContainerTestBase
{
    [Test]
    public void _01_BindPrimitive()
    {
        // いろんな型のインスタンスをバインド
        Container.Bind<int>().FromInstance(1);
        Container.Bind<float>().FromInstance(2f);
        Container.Bind<double>().FromInstance(3d);
        // BindInstance<T>()はBind<T>().FromInstance<T>()のショートハンド
        Container.BindInstance(4m);
        Container.BindInstance("str");
        Container.BindInstance(true);

        // Resolve()でバインドしたインスタンスを返す（何度呼んでもエラーにならない）
        for (var i = 0; i < 10; i++)
        {
            Assert.AreEqual(1, Container.Resolve<int>());
            Assert.AreEqual(2f, Container.Resolve<float>());
            Assert.AreEqual(3d, Container.Resolve<double>());
            Assert.AreEqual(4m, Container.Resolve<decimal>());
            Assert.AreEqual("str", Container.Resolve<string>());
            Assert.AreEqual(true, Container.Resolve<bool>());
        }
    }

    [Test]
    public void _02_BindMultiple()
    {
        // 同じ型のインスタンスを複数バインド
        Container.BindInstance("a");
        Container.BindInstance("b");
        Container.BindInstance("c");
        // BindInstances()でまとめてバインドできる
        Container.BindInstances("d", "e", "f");

        // Resolve<T>()はTが複数バインドされているとエラー
        AssertThrows<ZenjectException>(() => Container.Resolve<string>());
        // -> Found multiple matches when only one was expected for type 'string'.

        // ResolveAll<T>()はバインドした全てのTインスタンスをList<T>に詰めて返す
        var list = Container.ResolveAll<string>();
        Assert.AreEqual(6, list.Count);
        Assert.AreEqual("a", list[0]);
        Assert.AreEqual("b", list[1]);
        Assert.AreEqual("c", list[2]);
        Assert.AreEqual("d", list[3]);
        Assert.AreEqual("e", list[4]);
        Assert.AreEqual("f", list[5]);

        // BindInstances()は違う型のインスタンスもまとめてバインド可能
        Container.BindInstances(1, 2f, true);
        Assert.AreEqual(1, Container.Resolve<int>());
        Assert.AreEqual(2f, Container.Resolve<float>());
        Assert.AreEqual(true, Container.Resolve<bool>());
    }

    [Test]
    public void _03_ResolveWhenNoBindings()
    {
        // Resolve<T>()はTがバインドされていないとエラー
        AssertThrows<ZenjectException>(() => Container.Resolve<string>());
        // -> Unable to resolve 'string'.

        // 何もバインドしていない状態のResolveAll<T>()は空のList<T>を返す（エラーにはならない）
        Assert.AreEqual(0, Container.ResolveAll<string>().Count);

        // ResolveId(),ResolveIdAll()も同じ
        AssertThrows<ZenjectException>(() => Container.ResolveId<string>("foo"));
        // -> Unable to resolve 'string (ID: foo)'.
        Assert.AreEqual(0, Container.ResolveIdAll<string>("foo").Count);
    }

    [Test]
    public void _04_BindMultipleWithId()
    {
        // 同じ型のインスタンスをidつきで複数バインド
        Container.BindInstance("a").WithId("first");
        Container.BindInstance("b").WithId("second");

        // ResolveId()でidを指定して解決できる
        Assert.AreEqual("a", Container.ResolveId<string>("first"));
        Assert.AreEqual("b", Container.ResolveId<string>("second"));

        // Resolve()による解決ではidつきのバインドは無視される
        // この場合はidなしでマッチするバインドが存在しないので"Found multiple matches"のエラーではなく解決不可エラー
        AssertThrows<ZenjectException>(() => Container.Resolve<string>());
        // -> Unable to resolve 'string'.

        // ResolveAll()による解決も同様、idつきのバインドは無視される
        Assert.AreEqual(0, Container.ResolveAll<string>().Count);

        // 一つだけidなしでバインドするとResolve()で解決できる
        Container.BindInstance("c");
        Assert.AreEqual("c", Container.Resolve<string>());
    }

    [Test]
    public void _05_ResolveIdAll()
    {
        // idの指定はobject可（stringである必要はない）
        var id = new object();
        Container.BindInstance("a").WithId(id);
        Container.BindInstance("b");

        // ResolveIdAll<T>()はT&&idでマッチしたインスタンスをList<T>で返す
        var listWithId = Container.ResolveIdAll<string>(id);
        Assert.AreEqual(1, listWithId.Count);
        Assert.AreEqual("a", listWithId[0]);

        // 同じ型・同じidでバインドしてもエラーにならず、全てResolveIdAll()で取得できる
        Container.BindInstance("A").WithId(id);
        listWithId = Container.ResolveIdAll<string>(id);
        Assert.AreEqual(2, listWithId.Count);
        Assert.AreEqual("a", listWithId[0]);
        Assert.AreEqual("A", listWithId[1]);
    }

    [Test]
    public void _06_Unbind()
    {
        // バインド
        Container.BindInstance("str");
        Assert.AreEqual("str", Container.Resolve<string>());

        // Unbind()でバインド前の状態に戻る（Resolve()がエラーになる）
        Container.Unbind<string>();
        AssertThrows<ZenjectException>(() => Container.Resolve<string>());
        // -> Unable to resolve 'string'.

        // 複数バインド->アンバインド
        Container.BindInstances(1, 2, 3);
        Container.Unbind<int>();

        // Tを複数バインドしても一回のUnbind<T>()で全てアンバインドされる
        Assert.AreEqual(0, Container.ResolveAll<int>().Count);
        AssertThrows<ZenjectException>(() => Container.Resolve<int>());
        // -> Unable to resolve 'int'.
    }

    [Test]
    public void _07_UnbindId()
    {
        // idつきバインド
        var id = new object();
        Container.BindInstance("a").WithId(id);
        Assert.AreEqual("a", Container.ResolveId<string>(id));

        // UnbindId()でバインド前の状態に戻る（ResolveId()がエラーになる）
        Container.UnbindId<string>(id);
        AssertThrows<ZenjectException>(() => Container.ResolveId<string>(id));
        // -> Unable to resolve 'string'.

        // 複数バインド->アンバインド
        Container.BindInstance("b").WithId(id);
        Container.BindInstance("c").WithId(id);
        Container.UnbindId<string>(id);

        // Tをidつきで複数バインドしても一回のUnbindId<T>()で全てアンバインドされる
        Assert.AreEqual(0, Container.ResolveIdAll<string>(id).Count);
        AssertThrows<ZenjectException>(() => Container.ResolveId<string>(id));
        // -> Unable to resolve 'string'.
    }

    [Test]
    public void _08_UnbindAll()
    {
        Container.BindInstance("str");
        Container.BindInstance(1);
        Container.BindInstance(true);
        Container.BindInstance(100m).WithId("decimal");

        // UnbindAll()はバインド済みの全ての型をアンバインドする（idつき含む）
        Container.UnbindAll();
        AssertThrows<ZenjectException>(() => Container.Resolve<string>());
        AssertThrows<ZenjectException>(() => Container.Resolve<int>());
        AssertThrows<ZenjectException>(() => Container.Resolve<bool>());
        AssertThrows<ZenjectException>(() => Container.ResolveId<decimal>("decimal"));
        // -> Unable to resolve 'string'.
        // -> Unable to resolve 'int'.
        // -> Unable to resolve 'bool'.
        // -> Unable to resolve 'decimal (ID: decimal)'.
    }

    [Test]
    public void _09_UnbindWhenNoBindings()
    {
        // 何もバインドされていない状態での各アンバインドはエラーにならない
        Container.Unbind<string>();
        Container.Unbind<int>();
        Container.Unbind<bool>();
        Container.UnbindAll();
        Container.UnbindId<decimal>("");
    }
}