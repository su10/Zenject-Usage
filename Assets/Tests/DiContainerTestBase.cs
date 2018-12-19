using System;
using NUnit.Framework;
using UnityEngine;
using Zenject;

public abstract class DiContainerTestBase
{
    protected DiContainer Container;

    // テスト毎に新しくコンテナを作り直す
    [SetUp]
    public virtual void SetUp()
    {
        Container = new DiContainer();
    }

    protected void AssertThrows<T>(Action action) where T : Exception
    {
        Assert.Throws<T>(() =>
        {
            try
            {
                action.Invoke();
            }
            catch (T e)
            {
                // エラーの内容を出力
                if (e.InnerException != null)
                {
                    Debug.Log(e.InnerException.Message);
                }
                else
                {
                    Debug.Log(e.Message);
                }

                throw;
            }
        });
    }
}