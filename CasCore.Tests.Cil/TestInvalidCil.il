.class public auto ansi beforefieldinit DouglasDwyer.CasCore.Tests.Cil.TestInvalidCil
  extends [System.Runtime]System.Object
{
    .method public hidebysig static void TestNoRet() cil managed
    {
        .custom instance void [CasCore.Tests.Shared]DouglasDwyer.CasCore.Tests.TestExceptionAttribute::.ctor(class [System.Runtime]System.Type)
      = (
        01 00 72 53 79 73 74 65 6d 2e 49 6e 76 61 6c 69 // ..rSystem.Invali
        64 50 72 6f 67 72 61 6d 45 78 63 65 70 74 69 6f // dProgramExceptio
        6e 2c 20 53 79 73 74 65 6d 2e 52 75 6e 74 69 6d // n, System.Runtim
        65 2c 20 56 65 72 73 69 6f 6e 3d 31 30 2e 30 2e // e, Version=10.0.
        30 2e 30 2c 20 43 75 6c 74 75 72 65 3d 6e 65 75 // 0.0, Culture=neu
        74 72 61 6c 2c 20 50 75 62 6c 69 63 4b 65 79 54 // tral, PublicKeyT
        6f 6b 65 6e 3d 62 30 33 66 35 66 37 66 31 31 64 // oken=b03f5f7f11d
        35 30 61 33 61 00 00                            // 50a3a..
      )

        ldstr "Hello World!"
    }

    .method public hidebysig static void TestLdfldaOnInitOnly() cil managed
    {
        .custom instance void [CasCore.Tests.Shared]DouglasDwyer.CasCore.Tests.TestExceptionAttribute::.ctor(class [System.Runtime]System.Type)
      = (
        01 00 72 53 79 73 74 65 6d 2e 42 61 64 49 6d 61 // ..rSystem.BadIma
        67 65 46 6f 72 6d 61 74 45 78 63 65 70 74 69 6f // geFormatExceptio
        6e 2c 20 53 79 73 74 65 6d 2e 52 75 6e 74 69 6d // n, System.Runtim
        65 2c 20 56 65 72 73 69 6f 6e 3d 31 30 2e 30 2e // e, Version=10.0.
        30 2e 30 2c 20 43 75 6c 74 75 72 65 3d 6e 65 75 // 0.0, Culture=neu
        74 72 61 6c 2c 20 50 75 62 6c 69 63 4b 65 79 54 // tral, PublicKeyT
        6f 6b 65 6e 3d 62 30 33 66 35 66 37 66 31 31 64 // oken=b03f5f7f11d
        35 30 61 33 61 00 00                            // 50a3a..
      )

        ldsflda int32 [CasCore.Tests.Shared]DouglasDwyer.CasCore.Tests.Shared.SharedClass::AllowedReadonlyStaticField
        ldc.i4 -999
        stind.i4

        ret
    }

    .method public hidebysig static void TestLdfldOnInitOnly() cil managed
    {
        .custom instance void [CasCore.Tests.Shared]DouglasDwyer.CasCore.Tests.TestSuccessfulAttribute::.ctor()
      = (01 00 00 00 )

        // Note: there is a discrepancy here where the C# standard allows for modifying
        // readonly instance fields at runtime. CasCore currently disallows this via reflection,
        // but raw IL still can. Todo: either update JitIlVerification or relax CasCore's restrictions for this case.

        newobj instance void [CasCore.Tests.Shared]DouglasDwyer.CasCore.Tests.Shared.SharedClass::.ctor()
        ldflda int32 [CasCore.Tests.Shared]DouglasDwyer.CasCore.Tests.Shared.SharedClass::AllowedReadonlyField
        ldc.i4 -999
        stind.i4

        ret
    }
}
