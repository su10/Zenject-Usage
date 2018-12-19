using NUnit.Framework;
using Zenject;

public class _3_ParentingTest : DiContainerTestBase
{
    private DiContainer AncestorContainer;
    private DiContainer ParentContainer;
    private DiContainer SelfContainer;
    private DiContainer ChildContainer;

    public override void SetUp()
    {
        // ペアレンティング
        // Ancestor
        //  ↑
        // Parent
        //  ↑
        // Self
        //  ↑
        // Child
        AncestorContainer = new DiContainer();
        ParentContainer = new DiContainer(AncestorContainer);
        SelfContainer = new DiContainer(ParentContainer);
        ChildContainer = new DiContainer(SelfContainer);
    }

    [Test]
    public void _01_ParentContainers()
    {
        // ParentContainersには直接の親コンテナが入っている
        Assert.AreEqual(1, SelfContainer.ParentContainers.Length);
        Assert.AreEqual(ParentContainer, SelfContainer.ParentContainers[0]);
    }

    [Test]
    public void _02_AncestorContainers()
    {
        // AncestorContainersには親を辿って得られる全てのコンテナが入っている
        Assert.AreEqual(2, SelfContainer.AncestorContainers.Length);
        Assert.AreEqual(ParentContainer, SelfContainer.AncestorContainers[0]);
        Assert.AreEqual(AncestorContainer, SelfContainer.AncestorContainers[1]);
    }

    [Test]
    public void _03_MultipleParentsAndAncestors()
    {
        // ↓こんな感じのペアレンティングツリー
        // [p0a0,p0a1] [p1a0] [p2a0,p2a1]
        //         ＼     |     ／
        //          [p0, p1, p2]
        //            ＼  |  ／
        //              self

        var p0a0 = new DiContainer();
        var p0a1 = new DiContainer();
        // 親コンテナは複数持てる
        var p0 = new DiContainer(new[] {p0a0, p0a1});

        var p1a0 = new DiContainer();
        var p1 = new DiContainer(new[] {p1a0});

        var p2a0 = new DiContainer();
        var p2a1 = new DiContainer();
        var p2 = new DiContainer(new[] {p2a0, p2a1});

        var self = new DiContainer(new[] {p0, p1, p2});

        // ParentContainersには直接の親コンテナが全て入っている
        Assert.AreEqual(3, self.ParentContainers.Length);
        Assert.AreEqual(p0, self.ParentContainers[0]);
        Assert.AreEqual(p1, self.ParentContainers[1]);
        Assert.AreEqual(p2, self.ParentContainers[2]);

        // AncestorContainersには親を辿って得られる全てのコンテナが入っている（順番は幅優先探索）
        Assert.AreEqual(8, self.AncestorContainers.Length);
        Assert.AreEqual(p0, self.AncestorContainers[0]);
        Assert.AreEqual(p1, self.AncestorContainers[1]);
        Assert.AreEqual(p2, self.AncestorContainers[2]);
        Assert.AreEqual(p0a0, self.AncestorContainers[3]);
        Assert.AreEqual(p0a1, self.AncestorContainers[4]);
        Assert.AreEqual(p1a0, self.AncestorContainers[5]);
        Assert.AreEqual(p2a0, self.AncestorContainers[6]);
        Assert.AreEqual(p2a1, self.AncestorContainers[7]);
    }

    [Test]
    public void _04_BindParentAndSelfResolve()
    {
        // バインドしていないのでエラー
        AssertThrows<ZenjectException>(() => SelfContainer.Resolve<string>());
        // -> Unable to resolve 'string'.

        // 親コンテナにバインドされていれば自コンテナのResolve()は親を辿って解決される
        ParentContainer.BindInstance("parent");
        Assert.AreEqual("parent", SelfContainer.Resolve<string>());

        // 自コンテナにバインドされていればそのまま解決される
        SelfContainer.BindInstance("self");
        Assert.AreEqual("self", SelfContainer.Resolve<string>());

        // ResolveAll()による解決の探索には親コンテナツリーも含まれる
        var list = SelfContainer.ResolveAll<string>();
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual("self", list[0]);
        Assert.AreEqual("parent", list[1]);
    }

    [Test]
    public void _05_BindAncestorAndSelfResolve()
    {
        // バインドしていないのでエラー
        AssertThrows<ZenjectException>(() => SelfContainer.Resolve<string>());
        // -> Unable to resolve 'string'.

        // 親コンテナツリーのどこかにバインドされていれば自コンテナのResolve()は親を辿って解決される
        AncestorContainer.BindInstance("ancestor");
        Assert.AreEqual("ancestor", SelfContainer.Resolve<string>());

        // より親コンテナツリーの中で近いコンテナ優先で解決される
        ParentContainer.BindInstance("parent");
        Assert.AreEqual("parent", SelfContainer.Resolve<string>());
    }

    [Test]
    public void _06_MultipleParentsAndSelfResolve()
    {
        var parents = new[]
        {
            new DiContainer(),
            new DiContainer(),
            new DiContainer(),
        };

        var self = new DiContainer(parents);

        // 親コンテナ群にそれぞれバインド
        parents[0].BindInstance("p0");
        parents[1].BindInstance("p1");
        parents[2].BindInstance("p2");

        // ResolveAll<T>()は親コンテナ群にバインドされたものを集めてList<T>で返す
        var list = self.ResolveAll<string>();
        Assert.AreEqual(3, list.Count);
        Assert.AreEqual("p0", list[0]);
        Assert.AreEqual("p1", list[1]);
        Assert.AreEqual("p2", list[2]);

        // Resolve()はエラー（複数の親コンテナは一つの親コンテナとして振る舞う）
        AssertThrows<ZenjectException>(() => self.Resolve<string>());
        // -> Found multiple matches when only one was expected for type 'string'.

        // 自コンテナにバインドされていればエラーにならずに解決される
        self.BindInstance("self");
        Assert.AreEqual("self", self.Resolve<string>());

        // 自コンテナにバインドしたものはResolveAll()のListの先頭にくる
        list = self.ResolveAll<string>();
        Assert.AreEqual(4, list.Count);
        Assert.AreEqual("self", list[0]);
        Assert.AreEqual("p0", list[1]);
        Assert.AreEqual("p1", list[2]);
        Assert.AreEqual("p2", list[3]);
    }

    [Test]
    public void _07_MultipleAncestorsAndSelfResolve()
    {
        // ↓こんな感じのペアレンティングツリー
        // [p0a0,p0a1] [p1a0] [p2a0,p2a1]
        //         ＼     |     ／
        //          [p0, p1, p2]
        //            ＼  |  ／
        //              self

        var p0a0 = new DiContainer();
        var p0a1 = new DiContainer();
        var p0 = new DiContainer(new[] {p0a0, p0a1});

        var p1a0 = new DiContainer();
        var p1 = new DiContainer(new[] {p1a0});

        var p2a0 = new DiContainer();
        var p2a1 = new DiContainer();
        var p2 = new DiContainer(new[] {p2a0, p2a1});

        var self = new DiContainer(new[] {p0, p1, p2});

        // 祖先コンテナにバインド
        p1a0.BindInstance("p0a0");
        Assert.AreEqual("p0a0", self.Resolve<string>());

        // 別の親を子として持つ祖先コンテナにバインド
        p1a0.BindInstance("p1a0");

        // Resolve()は"Found multiple matches"のエラー（深さが同じ複数の祖先コンテナは一つの祖先コンテナとして振る舞う）
        AssertThrows<ZenjectException>(() => self.Resolve<string>());
        // -> Found multiple matches when only one was expected for type 'string'.

        // ResolveAll<T>()はペアレンティングツリーにバインドされたもの全てを集めてList<T>で返す（幅優先探索で解決）
        p2.BindInstance("p2");
        var list = self.ResolveAll<string>();
        Assert.AreEqual(3, list.Count);
        Assert.AreEqual("p2", list[0]);
        Assert.AreEqual("p0a0", list[1]);
        Assert.AreEqual("p1a0", list[2]);
    }

    [Test]
    public void _08_MultipleParentsAndAncestorsAndSelfResolve()
    {
        // ↓こんな感じのペアレンティングツリー
        // [p0a0,p0a1] [p1a0] [p2a0,p2a1]
        //         ＼     |     ／
        //          [p0, p1, p2]
        //            ＼  |  ／
        //              self

        var p0a0 = new DiContainer();
        var p0a1 = new DiContainer();
        var p0 = new DiContainer(new[] {p0a0, p0a1});

        var p1a0 = new DiContainer();
        var p1 = new DiContainer(new[] {p1a0});

        var p2a0 = new DiContainer();
        var p2a1 = new DiContainer();
        var p2 = new DiContainer(new[] {p2a0, p2a1});

        var self = new DiContainer(new[] {p0, p1, p2});

        // 祖先コンテナにバインド
        p1a0.BindInstance("p1a0");
        Assert.AreEqual("p1a0", self.Resolve<string>());

        // 親コンテナにバインド
        // WARNING: 深さが同じコンテナ群にバインドした場合と違ってエラーにならない
        p0.BindInstance("p0");
        Assert.AreEqual("p0", self.Resolve<string>());

        // ResolveAll<T>()はペアレンティングツリーにバインドされたもの全てを集めてList<T>で返す（幅優先探索で解決）
        var list = self.ResolveAll<string>();
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual("p0", list[0]);
        Assert.AreEqual("p1a0", list[1]);
    }

    [Test]
    public void _09_UnbindAndUnbindAll()
    {
        // Unbind(),UnbindAll()はResolve()時の親コンテナツリーの探索に影響を与えない
        ParentContainer.BindInstance("parent");
        SelfContainer.Unbind<string>();
        Assert.AreEqual("parent", SelfContainer.Resolve<string>());
        SelfContainer.UnbindAll();
        Assert.AreEqual("parent", SelfContainer.Resolve<string>());

        // ペアレンティングが解除されたりもしない
        Assert.AreEqual(1, SelfContainer.ParentContainers.Length);
        Assert.AreEqual(ParentContainer, SelfContainer.ParentContainers[0]);

        Assert.AreEqual(2, SelfContainer.AncestorContainers.Length);
        Assert.AreEqual(ParentContainer, SelfContainer.AncestorContainers[0]);
        Assert.AreEqual(AncestorContainer, SelfContainer.AncestorContainers[1]);
    }

    [Test]
    public void _10_BindChildAndSelfResolve()
    {
        // 子コンテナのバインドは親コンテナへ影響を与えない
        ChildContainer.BindInstance("child");
        AssertThrows<ZenjectException>(() => SelfContainer.Resolve<string>());
        // -> Unable to resolve 'string'.
        Assert.AreEqual(0, SelfContainer.ResolveAll<string>().Count);
    }
}