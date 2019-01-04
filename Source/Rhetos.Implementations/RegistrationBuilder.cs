using System;
using Autofac;

namespace Rhetos
{
    public class RegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> : IRegistrationBuilder
    {
        Autofac.Builder.IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> _rgistrationBuilder;

        public RegistrationBuilder(Autofac.Builder.IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> rgistrationBuilder)
        {
            _rgistrationBuilder = rgistrationBuilder;
        }

        public IRegistrationBuilder As<TService>()
        {
            _rgistrationBuilder.As<TService>();
            return this;
        }

        public IRegistrationBuilder SingleInstance()
        {
            _rgistrationBuilder.SingleInstance();
            return this;
        }
    }
}
