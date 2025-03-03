using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

public static class Logger
{
    // Klasa zapewnia funkcje do zapisywania zdarzen w pliku
    private static string lf = "historia.log";

    public static void Log(string t)
    {
        // Dodaje opis do pliku razem z czasem
        using (var x = new StreamWriter(lf, true))
        {
            x.WriteLine(DateTime.Now + " :: " + t);
        }
    }
}

public class Haslo
{
    // Reprezentuje podstawowe dane hasla
    public string Serwis;
    public string Login;
    public string Zaszyfrowane;

    public virtual void PokazInfo()
    {
        // Metoda wyswietla podstawowe dane
        Console.WriteLine("Serwis: " + Serwis + " | Login: " + Login + " | (Zaszyfrowane) " + Zaszyfrowane);
    }
}

public class HasloPremium : Haslo
{
    // Rozszerzona klasa hasla o komentarz
    public string Komentarz;

    public override void PokazInfo()
    {
        // Nadpisana metoda wyswietla informacje premium
        Console.WriteLine("[PREMIUM] " + Serwis + " (" + Komentarz + "): " + Zaszyfrowane);
    }
}

public abstract class PlikBazyDanych
{
    // Klasa abstrakcyjna do obslugi plikow bazy danych
    public abstract void Zapisz(Haslo h);
    public abstract List<Haslo> Wczytaj();
    public abstract void Nadpisz(List<Haslo> lista);
}

public class PlikTxt : PlikBazyDanych
{
    // Klasa obslugujaca format txt do przechowywania hasel
    private string sciezka;

    public PlikTxt(string sciezkaPliku)
    {
        sciezka = sciezkaPliku;
        if (!File.Exists(sciezka))
        {
            File.Create(sciezka).Close();
        }
    }

    public override void Zapisz(Haslo h)
    {
        // Zapisuje jedna linie dla kazdego obiektu
        using (var w = new StreamWriter(sciezka, true))
        {
            w.WriteLine(h.Serwis + "|" + h.Login + "|" + h.Zaszyfrowane);
        }
    }

    public override List<Haslo> Wczytaj()
    {
        // Wczytuje dane z pliku i zamienia je na liste obiektow
        var lista = new List<Haslo>();
        var linie = File.ReadAllLines(sciezka);
        foreach (var lin in linie)
        {
            var parts = lin.Split('|');
            if (parts.Length >= 3)
            {
                var obiekt = new Haslo();
                obiekt.Serwis = parts[0];
                obiekt.Login = parts[1];
                obiekt.Zaszyfrowane = parts[2];
                lista.Add(obiekt);
            }
        }
        return lista;
    }

    public override void Nadpisz(List<Haslo> lista)
    {
        // Nadpisuje caly plik nowymi danymi
        using (var w = new StreamWriter(sciezka, false))
        {
            foreach (var h in lista)
            {
                w.WriteLine(h.Serwis + "|" + h.Login + "|" + h.Zaszyfrowane);
            }
        }
    }
}

public static class Szyfr
{
    // Klasa zapewnia funkcje szyfrowania i deszyfrowania
    static byte[] klucz = Encoding.UTF8.GetBytes("12345678901234561234567890123456");
    static byte[] iv = Encoding.UTF8.GetBytes("6543210987654321");

    public static string Szyfruj(string jawne)
    {
        // Metoda koduje dane AES
        if (string.IsNullOrWhiteSpace(jawne)) return "";
        using (Aes aes = Aes.Create())
        {
            aes.Key = klucz;
            aes.IV = iv;
            var enc = aes.CreateEncryptor();
            var bajty = Encoding.UTF8.GetBytes(jawne);
            var wynik = enc.TransformFinalBlock(bajty, 0, bajty.Length);
            return Convert.ToBase64String(wynik);
        }
    }

    public static string Deszyfruj(string en)
    {
        // Metoda deszyfruje dane
        if (string.IsNullOrEmpty(en)) return "";
        using (Aes aes = Aes.Create())
        {
            aes.Key = klucz;
            aes.IV = iv;
            var dec = aes.CreateDecryptor();
            var encb = Convert.FromBase64String(en);
            var r = dec.TransformFinalBlock(encb, 0, encb.Length);
            return Encoding.UTF8.GetString(r);
        }
    }
}

public class PasswordManager
{
    // Klasa zarzadza operacjami na haslach
    private PlikBazyDanych baza;

    public PasswordManager(PlikBazyDanych p)
    {
        this.baza = p;
    }

    public void Dodaj()
    {
        // Dodaje nowe haslo do pliku
        Console.Write("Serwis: ");
        string s = Console.ReadLine();
        Console.Write("Login: ");
        string l = Console.ReadLine();
        Console.Write("Haslo (jawne): ");
        string h = Console.ReadLine();
        var enc = Szyfr.Szyfruj(h);

        var ob = new Haslo
        {
            Serwis = s,
            Login = l,
            Zaszyfrowane = enc
        };
        baza.Zapisz(ob);
        Logger.Log("Dodano haslo -> serwis: " + s);
    }

    public void PokazWszystkie()
    {
        // Wyswietla wszystkie hasla z bazy
        var all = baza.Wczytaj();
        if (all.Count < 1)
        {
            Console.WriteLine("Brak wpisow!");
            return;
        }
        foreach (var x in all)
        {
            string j = Szyfr.Deszyfruj(x.Zaszyfrowane);
            Console.WriteLine($"{x.Serwis} -> login: {x.Login}, haslo: {j}");
        }
    }

    public void UsunCos()
    {
        // Usuwa hasla dotyczace podanego serwisu
        Console.Write("Podaj serwis do usuniecia: ");
        string s = Console.ReadLine();
        var all = baza.Wczytaj();
        int ile = all.RemoveAll(a => a.Serwis.Equals(s, StringComparison.OrdinalIgnoreCase));
        if (ile > 0)
        {
            baza.Nadpisz(all);
            Logger.Log("Usunieto " + ile + " rekordow (serwis=" + s + ")");
            Console.WriteLine("Usunieto " + ile + " rekord(ow).");
        }
        else
        {
            Console.WriteLine("Nie znaleziono wpisu.");
        }
    }

    public void Edytuj()
    {
        // Edycja wybranych wartosci dla jednego wpisu
        Console.Write("Serwis do edycji: ");
        string s = Console.ReadLine();
        var all = baza.Wczytaj();
        var rekord = all.Find(x => x.Serwis.Equals(s, StringComparison.OrdinalIgnoreCase));
        if (rekord == null)
        {
            Console.WriteLine("Brak wpisu.");
            return;
        }
        Console.WriteLine("Obecny login: " + rekord.Login);
        Console.Write("Nowy login (ENTER = bez zmian): ");
        string nowyLog = Console.ReadLine();
        if (!string.IsNullOrEmpty(nowyLog))
        {
            rekord.Login = nowyLog;
        }
        string jawneOld = Szyfr.Deszyfruj(rekord.Zaszyfrowane);
        Console.WriteLine("Obecne haslo: " + jawneOld);
        Console.Write("Nowe haslo (ENTER = bez zmian): ");
        string nHas = Console.ReadLine();
        if (!string.IsNullOrEmpty(nHas))
        {
            rekord.Zaszyfrowane = Szyfr.Szyfruj(nHas);
        }
        baza.Nadpisz(all);
        Logger.Log("Edytowano serwis " + s);
        Console.WriteLine("Zapisano zmiany.");
    }
}

class Program
{
    // Program glowny obslugujacy konsole
    static void Main(string[] args)
    {
        string plik = "passwordy.txt";
        PlikBazyDanych db = new PlikTxt(plik);
        var pm = new PasswordManager(db);

        while (true)
        {
            Console.WriteLine("\n--- MENEDZER HASEL ---");
            Console.WriteLine("1) Dodaj haslo");
            Console.WriteLine("2) Lista hasel");
            Console.WriteLine("3) Usun haslo");
            Console.WriteLine("4) Edytuj haslo");
            Console.WriteLine("5) Wyjscie");
            Console.Write("Wybor: ");
            string op = Console.ReadLine();

            switch (op)
            {
                case "1":
                    pm.Dodaj();
                    break;
                case "2":
                    pm.PokazWszystkie();
                    break;
                case "3":
                    pm.UsunCos();
                    break;
                case "4":
                    pm.Edytuj();
                    break;
                case "5":
                    Console.WriteLine("Koniec. Papa!");
                    return;
                default:
                    Console.WriteLine("Zly wybor!");
                    break;
            }
        }
    }
}
