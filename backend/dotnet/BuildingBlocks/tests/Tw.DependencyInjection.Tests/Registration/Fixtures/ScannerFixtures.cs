using Tw.DependencyInjection.Registration.Attributes;

namespace Tw.DependencyInjection.Tests.Registration.Fixtures;

// ---- interfaces ----

public interface IOrderService { }
public interface IPaymentService { }
public interface IHandler { }
public interface IFoo { }
public interface IBar { }
public interface IRepository<T> { }

// ---- lifecycle + business interface ----

/// <summary>有业务接口的作用域服务，默认应暴露 IOrderService。</summary>
public class OrderService : IOrderService, IScopedDependency { }

/// <summary>无业务接口的作用域服务，默认应暴露实现类型本身。</summary>
public class ConcreteWorker : IScopedDependency { }

// ---- [DisableAutoRegistration] ----

[DisableAutoRegistration]
public class DisabledService : IScopedDependency { }

// ---- [ExposeServices] 显式列表 ----

[ExposeServices(typeof(IFoo))]
public class FooBar : IFoo, IBar, ITransientDependency { }

// ---- [ExposeServices] IncludeSelf = true ----

[ExposeServices(typeof(IFoo), IncludeSelf = true)]
public class FooBarSelf : IFoo, ITransientDependency { }

// ---- [KeyedService] ----

[KeyedService("premium")]
public class PremiumPayment : IPaymentService, ISingletonDependency { }

// ---- [CollectionService] ----

[CollectionService(Order = 10)]
public class FirstHandler : IHandler, IScopedDependency { }

// ---- [ReplaceService] ----

[ReplaceService(Order = 5)]
public class ReplacementOrderService : IOrderService, IScopedDependency { }

// ---- open generic exposure (valid) ----

[ExposeServices(typeof(IRepository<>))]
public class Repository<T> : IRepository<T>, IScopedDependency { }

// ---- open generic + [KeyedService] = startup error ----

[KeyedService("primary")]
public class KeyedRepository<T> : IRepository<T>, IScopedDependency { }

// ---- multiple lifecycle markers = startup error ----

public class MultiLifecycleService : IScopedDependency, ISingletonDependency { }

// ---- transient without business interface ----

public class TransientWorker : ITransientDependency { }

// ---- singleton with business interface ----

public class SingletonPayment : IPaymentService, ISingletonDependency { }
