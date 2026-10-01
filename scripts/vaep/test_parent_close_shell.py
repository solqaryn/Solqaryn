"""Offline shell regression: never calls GitHub or publishes a ref."""
from pathlib import Path
import json
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / '.github/scripts/vaep-parent-close.sh'
FUNCTIONS = SCRIPT.read_text().split('if [[ "${1:-}" == "--self-test" ]]', 1)[0]


def bash(body, cwd=None):
    return subprocess.run(['bash', '-c', FUNCTIONS + '\n' + body], cwd=cwd or ROOT,
                          capture_output=True, text=True, timeout=15)


class ParentCloseShellTests(unittest.TestCase):
    def test_end_of_roadmap_keeps_open_only_admission(self):
        from test_parent_transition import catalog, access
        from parent_transition import transition
        _, a = transition(catalog("N5.1.B"), access("OPEN_ONLY_SELFTEST"), {"N5.1.B"}, "r.json", "a"*40, "now", True)
        self.assertEqual(a["newDispatchAdmission"], "OPEN")
        self.assertIn("OPEN_ONLY", a["reason"])

    def test_bash_syntax(self):
        self.assertEqual(subprocess.run(['bash', '-n', str(SCRIPT)], capture_output=True).returncode, 0)

    def test_newer_failure_blocks_old_success(self):
        runs = {'workflow_runs': [dict(head_sha='a'*40, head_branch='dev', event='push', created_at='2026-01-01', id=1, status='completed', conclusion='success'), dict(head_sha='a'*40, head_branch='dev', event='push', created_at='2026-01-02', id=2, status='completed', conclusion='failure')]}
        r = bash("latest_push_success " + 'a'*40 + " <<'DATA'\n" + json.dumps(runs) + '\nDATA')
        self.assertNotEqual(r.returncode, 0)

    def test_running_latest_blocks(self):
        runs = {'workflow_runs': [dict(head_sha='a'*40, head_branch='dev', event='push', created_at='2026', id=1, status='in_progress', conclusion=None)]}
        self.assertNotEqual(bash("latest_push_success " + 'a'*40 + " <<'DATA'\n" + json.dumps(runs) + '\nDATA').returncode, 0)

    def test_wrong_head_does_not_pass(self):
        runs = {'workflow_runs': [dict(head_sha='b'*40, head_branch='dev', event='push', created_at='2026', id=1, status='completed', conclusion='success')]}
        self.assertNotEqual(bash("latest_push_success " + 'a'*40 + " <<'DATA'\n" + json.dumps(runs) + '\nDATA').returncode, 0)

    def test_both_migration_locations(self):
        for path in ['backend/src/Infrastructure/Migrations/X.cs', 'backend/src/Infrastructure/Persistence/Migrations/X.cs']:
            payload = json.dumps({'files': [{'filename': path}]})
            self.assertEqual(bash("api() { printf '%s\\n' '" + payload + "'; }; migration_gate_applicable a").returncode, 0)

    def test_head_race_never_reports_promotion(self):
        r = bash("current_head() { echo changed; }; api() { echo UNEXPECTED_API >&2; return 99; }; publish_promotion original c a parent next")
        self.assertEqual(r.returncode, 0)
        self.assertIn('DEFERRED=HEAD_CHANGED', r.stdout)
        self.assertNotIn('PROMOTED=true', r.stdout)
        self.assertNotIn('UNEXPECTED_API', r.stderr)

    def test_product_file_is_not_control_plane(self):
        self.assertNotEqual(bash('is_control_plane_path backend/src/API/Program.cs').returncode, 0)

    def test_functional_head_uses_real_local_git_history(self):
        with tempfile.TemporaryDirectory() as root:
            def git(*args):
                return subprocess.check_output(['git', *args], cwd=root, text=True).strip()
            git('init', '-q'); git('config', 'user.email', 'test@example.invalid'); git('config', 'user.name', 'Test')
            Path(root, 'product.cs').write_text('product')
            git('add', '.'); git('commit', '-qm', 'product')
            product = git('rev-parse', 'HEAD')
            Path(root, 'docs').mkdir(); Path(root, 'docs', 'evidence.md').write_text('evidence')
            git('add', '.'); git('commit', '-qm', 'control')
            head = git('rev-parse', 'HEAD')
            r = bash('functional_head ' + head, cwd=root)
            self.assertEqual(r.returncode, 0, r.stderr)
            self.assertEqual(r.stdout.strip(), product)


if __name__ == '__main__':
    unittest.main()
